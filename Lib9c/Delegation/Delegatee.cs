#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegatee<T, TSelf> : IDelegatee
        where T : Delegator<TSelf, T>
        where TSelf : Delegatee<T, TSelf>
    {
        protected readonly byte[] BondId = new byte[] { 0x42 };                  // `B`
        protected readonly byte[] UnbondLockInId = new byte[] { 0x55 };          // `U`
        protected readonly byte[] RebondGraceId = new byte[] { 0x52 };           // `R`
        protected readonly byte[] LumpSumRewardsRecordId = new byte[] { 0x4c };  // `L`
        protected readonly byte[] RewardCollectorId = new byte[] { 0x63 };       // `c`
        protected readonly byte[] RewardDistributorId = new byte[] { 0x64 };     // `d`
        protected readonly byte[] PoolId = new byte[] { 0x70 };                  // `p`

        private readonly IDelegationRepository? _repository;
        private ImmutableSortedSet<UnbondingRef> _unbondingRefs;

        public Delegatee(Address address, IDelegationRepository? repository = null)
        {
            Address = address;
            Delegators = ImmutableSortedSet<Address>.Empty;
            TotalDelegated = DelegationCurrency * 0;
            TotalShares = BigInteger.Zero;
            _unbondingRefs = ImmutableSortedSet<UnbondingRef>.Empty;
            _repository = repository;
        }

        public Delegatee(Address address, IValue bencoded, IDelegationRepository? repository = null)
            : this(address, (List)bencoded, repository)
        {
        }

        public Delegatee(Address address, List bencoded, IDelegationRepository? repository = null)
            : this(
                  address,
                  ((List)bencoded[0]).Select(item => new Address(item)),
                  new FungibleAssetValue(bencoded[1]),
                  (Integer)bencoded[2],
                  ((List)bencoded[3]).Select(item => new UnbondingRef(item)),
                  repository)
        {
        }

        private Delegatee(
            Address address,
            IEnumerable<Address> delegators,
            FungibleAssetValue totalDelegated,
            BigInteger totalShares,
            IEnumerable<UnbondingRef> unbondingRefs,
            IDelegationRepository? repository)
        {
            if (!totalDelegated.Currency.Equals(DelegationCurrency))
            {
                throw new InvalidOperationException("Invalid currency.");
            }

            if (totalDelegated.Sign < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalDelegated),
                    totalDelegated,
                    "Total delegated must be non-negative.");
            }

            if (totalShares.Sign < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalShares),
                    totalShares,
                    "Total shares must be non-negative.");
            }

            Address = address;
            Delegators = delegators.ToImmutableSortedSet();
            TotalDelegated = totalDelegated;
            TotalShares = totalShares;
            _unbondingRefs = unbondingRefs.ToImmutableSortedSet();
            _repository = repository;
        }

        public Address Address { get; }

        public abstract Currency DelegationCurrency { get; }

        public abstract Currency RewardCurrency { get; }

        public abstract Address DelegationPoolAddress { get; }

        public abstract long UnbondingPeriod { get; }

        public abstract byte[] DelegateeId { get; }

        public abstract int MaxUnbondLockInEntries { get; }

        public abstract int MaxRebondGraceEntries { get; }

        public abstract BigInteger SlashFactor { get; }

        public Address RewardCollectorAddress => DeriveAddress(RewardCollectorId);

        public Address RewardDistributorAddress => DeriveAddress(RewardDistributorId);

        public ImmutableSortedSet<Address> Delegators { get; private set; }

        public FungibleAssetValue TotalDelegated { get; private set; }

        public BigInteger TotalShares { get; private set; }

        public IDelegationRepository? Repository => _repository;

        public virtual List Bencoded => List.Empty
            .Add(new List(Delegators.Select(delegator => delegator.Bencoded)))
            .Add(TotalDelegated.Serialize())
            .Add(TotalShares)
            .Add(new List(_unbondingRefs.Select(unbondingRef => unbondingRef.Bencoded)));

        IValue IBencodable.Bencoded => Bencoded;

        public BigInteger ShareToBond(FungibleAssetValue fav)
            => TotalShares.IsZero
                ? fav.RawValue
                : TotalShares * fav.RawValue / TotalDelegated.RawValue;

        public FungibleAssetValue FAVToUnbond(BigInteger share)
            => TotalShares == share
                ? TotalDelegated
                : (TotalDelegated * share).DivRem(TotalShares).Quotient;

        BigInteger IDelegatee.Bond(
            IDelegator delegator, FungibleAssetValue fav, long height)
            => Bond((T)delegator, fav, height);

        FungibleAssetValue IDelegatee.Unbond(
            IDelegator delegator, BigInteger share, long height)
            => Unbond((T)delegator, share, height);

        void IDelegatee.DistributeReward(IDelegator delegator, long height)
            => DistributeReward((T)delegator, height);

        public virtual BigInteger Bond(T delegator, FungibleAssetValue fav, long height)
        {
            CannotMutateRelationsWithoutRepository(delegator);
            DistributeReward(delegator, height);

            if (!fav.Currency.Equals(DelegationCurrency))
            {
                throw new InvalidOperationException(
                    "Cannot bond with invalid currency.");
            }

            Bond bond = _repository!.GetBond(this, delegator.Address);
            BigInteger share = ShareToBond(fav);
            bond = bond.AddShare(share);
            Delegators = Delegators.Add(delegator.Address);
            TotalShares += share;
            TotalDelegated += fav;
            _repository.SetBond(bond);
            StartNewRewardPeriod(height);

            return share;
        }

        public virtual FungibleAssetValue Unbond(T delegator, BigInteger share, long height)
        {
            CannotMutateRelationsWithoutRepository(delegator);
            DistributeReward(delegator, height);
            if (TotalShares.IsZero || TotalDelegated.RawValue.IsZero)
            {
                throw new InvalidOperationException(
                    "Cannot unbond without bonding.");
            }

            Bond bond = _repository!.GetBond(this, delegator.Address);
            FungibleAssetValue fav = FAVToUnbond(share);
            bond = bond.SubtractShare(share);
            if (bond.Share.IsZero)
            {
                Delegators = Delegators.Remove(delegator.Address);
            }

            TotalShares -= share;
            TotalDelegated -= fav;
            _repository.SetBond(bond);
            StartNewRewardPeriod(height);

            return fav;
        }

        public virtual void DistributeReward(T delegator, long height)
        {
            CannotMutateRelationsWithoutRepository(delegator);
            BigInteger share = _repository!.GetBond(this, delegator.Address).Share;
            IEnumerable<LumpSumRewardsRecord> lumpSumRewardsRecords =
                GetLumpSumRewardsRecords(delegator.LastRewardHeight);
            FungibleAssetValue reward = CalculateReward(share, lumpSumRewardsRecords);
            if (reward.Sign > 0)
            {
                _repository.TransferAsset(RewardDistributorAddress, delegator.Address, reward);
            }

            delegator.UpdateLastRewardHeight(height);
        }

        public virtual void CollectRewards(long height)
        {
            CannotMutateRelationsWithoutRepository();
            FungibleAssetValue rewards = _repository!.GetBalance(RewardCollectorAddress, RewardCurrency);
            _repository!.AddLumpSumRewards(this, height, rewards);
            _repository!.TransferAsset(RewardCollectorAddress, RewardDistributorAddress, rewards);
        }

        public void Slash(long infractionHeight)
        {
            CannotMutateRelationsWithoutRepository();
            foreach (var item in _unbondingRefs)
            {
                var unbonding = UnbondingFactory.GetUnbondingFromRef(item, _repository)
                    .Slash(SlashFactor, infractionHeight);

                if (unbonding.IsEmpty)
                {
                    RemoveUnbondingRef(item);
                }

                switch (unbonding)
                {
                    case UnbondLockIn unbondLockIn:
                        _repository!.SetUnbondLockIn(unbondLockIn);
                        break;
                    case RebondGrace rebondGrace:
                        _repository!.SetRebondGrace(rebondGrace);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid unbonding type.");
                }
            }
        }

        public void AddUnbondingRef(UnbondingRef unbondingRef)
        {
            _unbondingRefs = _unbondingRefs.Add(unbondingRef);
        }

        public void RemoveUnbondingRef(UnbondingRef unbondingRef)
        {
            _unbondingRefs = _unbondingRefs.Remove(unbondingRef);
        }

        public virtual Address BondAddress(Address delegatorAddress)
            => DeriveAddress(BondId, delegatorAddress);

        public virtual Address UnbondLockInAddress(Address delegatorAddress)
            => DeriveAddress(UnbondLockInId, delegatorAddress);

        public virtual Address RebondGraceAddress(Address delegatorAddress)
            => DeriveAddress(RebondGraceId, delegatorAddress);

        public virtual Address CurrentLumpSumRewardsRecordAddress()
            => DeriveAddress(LumpSumRewardsRecordId);

        public virtual Address LumpSumRewardsRecordAddress(long height)
            => DeriveAddress(LumpSumRewardsRecordId, BitConverter.GetBytes(height));


        public override bool Equals(object? obj)
            => obj is IDelegatee other && Equals(other);

        public virtual bool Equals(IDelegatee? other)
            => ReferenceEquals(this, other)
            || (other is Delegatee<T, TSelf> delegatee
            && (GetType() != delegatee.GetType())
            && Address.Equals(delegatee.Address)
            && DelegationCurrency.Equals(delegatee.DelegationCurrency)
            && RewardCurrency.Equals(delegatee.RewardCurrency)
            && DelegationPoolAddress.Equals(delegatee.DelegationPoolAddress)
            && UnbondingPeriod == delegatee.UnbondingPeriod
            && RewardCollectorAddress.Equals(delegatee.RewardCollectorAddress)
            && RewardDistributorAddress.Equals(delegatee.RewardDistributorAddress)
            && Delegators.SequenceEqual(delegatee.Delegators)
            && TotalDelegated.Equals(delegatee.TotalDelegated)
            && TotalShares.Equals(delegatee.TotalShares)
            && _unbondingRefs.SequenceEqual(delegatee._unbondingRefs)
            && DelegateeId.SequenceEqual(delegatee.DelegateeId));

        public override int GetHashCode()
            => Address.GetHashCode();

        protected Address DeriveAddress(byte[] typeId, Address address)
            => DeriveAddress(typeId, address.ByteArray);

        protected Address DeriveAddress(byte[] typeId, IEnumerable<byte>? bytes = null)
        {
            byte[] hashed;
            using (HMACSHA1 hmac = new(DelegateeId.Concat(typeId).ToArray()))
            {
                hashed = hmac.ComputeHash(
                    Address.ByteArray.Concat(bytes ?? Array.Empty<byte>()).ToArray());
            }

            return new Address(hashed);
        }

        private void StartNewRewardPeriod(long height)
        {
            CannotMutateRelationsWithoutRepository();
            LumpSumRewardsRecord? currentRecord = _repository!.GetCurrentLumpSumRewardsRecord(this);
            long? lastStartHeight = null;
            if (currentRecord is LumpSumRewardsRecord lastRecord)
            {
                lastStartHeight = lastRecord.StartHeight;
                if (lastStartHeight == height)
                {
                    currentRecord = new(
                        currentRecord.Address,
                        currentRecord.StartHeight,
                        TotalShares,
                        RewardCurrency,
                        currentRecord.LastStartHeight);

                    _repository.SetLumpSumRewardsRecord(currentRecord);
                    return;
                }

                _repository.SetLumpSumRewardsRecord(
                    lastRecord.MoveAddress(
                        LumpSumRewardsRecordAddress(lastRecord.StartHeight)));
            }

            LumpSumRewardsRecord newRecord = new(
                CurrentLumpSumRewardsRecordAddress(),
                height,
                TotalShares,
                RewardCurrency,
                lastStartHeight);

            _repository.SetLumpSumRewardsRecord(newRecord);
        }

        private FungibleAssetValue CalculateReward(
            BigInteger share,
            IEnumerable<LumpSumRewardsRecord> lumpSumRewardsRecords)
        {
            FungibleAssetValue reward = RewardCurrency * 0;
            long? linkedStartHeight = null;

            foreach (LumpSumRewardsRecord record in lumpSumRewardsRecords)
            {
                if (!(record.StartHeight is long startHeight))
                {
                    throw new ArgumentException("lump sum reward record wasn't started.");
                }

                if (linkedStartHeight is long startHeightFromHigher
                    && startHeightFromHigher != startHeight)
                {
                    throw new ArgumentException("lump sum reward record was started.");
                }

                reward += record.RewardsDuringPeriod(share);
                linkedStartHeight = record.LastStartHeight;

                if (linkedStartHeight == -1)
                {
                    break;
                }
            }

            return reward;
        }

        private List<LumpSumRewardsRecord> GetLumpSumRewardsRecords(long? lastRewardHeight)
        {
            CannotMutateRelationsWithoutRepository();
            List<LumpSumRewardsRecord> records = new();
            if (lastRewardHeight is null
                || !(_repository!.GetCurrentLumpSumRewardsRecord(this) is LumpSumRewardsRecord record))
            {
                return records;
            }

            while (record.StartHeight >= lastRewardHeight)
            {
                records.Add(record);

                if (!(record.LastStartHeight is long lastStartHeight))
                {
                    break;
                }

                record = _repository.GetLumpSumRewardsRecord(this, lastStartHeight)
                    ?? throw new InvalidOperationException(
                        $"Lump sum rewards record for #{lastStartHeight} is missing");
            }

            return records;
        }

        private void CannotMutateRelationsWithoutRepository(T delegator)
        {
            CannotMutateRelationsWithoutRepository();
            if (!_repository!.Equals(delegator.Repository))
            {
                throw new InvalidOperationException(
                    "Cannot mutate with different repository.");
            }
        }

        private void CannotMutateRelationsWithoutRepository()
        {
            if (_repository is null)
            {
                throw new InvalidOperationException(
                    "Cannot mutate without repository.");
            }
        }
    }
}
