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
        protected readonly byte[] BondId = new byte[] { 0x44 };                  // `D`
        protected readonly byte[] UnbondLockInId = new byte[] { 0x55 };          // `U`
        protected readonly byte[] RebondGraceId = new byte[] { 0x52 };           // `R`
        protected readonly byte[] LumpSumRewardsRecordId = new byte[] { 0x4c };  // `L`
        protected readonly byte[] RewardPoolId = new byte[] { 0x72 };            // `r`
        protected readonly byte[] PoolId = new byte[] { 0x70 };                  // `p`

        private readonly IDelegationRepository? _repository;

        public Delegatee(Address address, IDelegationRepository? repository = null)
        {
            Address = address;
            Delegators = ImmutableSortedSet<Address>.Empty;
            TotalDelegated = Currency * 0;
            TotalShares = BigInteger.Zero;
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
                  repository)
        {
        }

        private Delegatee(
            Address address,
            IEnumerable<Address> delegators,
            FungibleAssetValue totalDelegated,
            BigInteger totalShares,
            IDelegationRepository? repository)
        {
            if (!totalDelegated.Currency.Equals(Currency))
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
            _repository = repository;
        }

        public Address Address { get; }

        public abstract Currency Currency { get; }

        public abstract Currency RewardCurrency { get; }

        public abstract Address DelegationPoolAddress { get; }

        public abstract long UnbondingPeriod { get; }

        public abstract byte[] DelegateeId { get; }

        public abstract int MaxUnbondLockInEntries { get; }

        public abstract int MaxRebondGraceEntries { get; }

        public Address RewardPoolAddress => DeriveAddress(RewardPoolId);

        public ImmutableSortedSet<Address> Delegators { get; private set; }

        public FungibleAssetValue TotalDelegated { get; private set; }

        public BigInteger TotalShares { get; private set; }

        public IDelegationRepository? Repository => _repository;

        public List Bencoded => List.Empty
            .Add(new List(Delegators.Select(delegator => delegator.Bencoded)))
            .Add(TotalDelegated.Serialize())
            .Add(TotalShares);

        IValue IBencodable.Bencoded => Bencoded;

        public BigInteger ShareToBond(FungibleAssetValue fav)
            => TotalShares.IsZero
                ? fav.RawValue
                : TotalShares * fav.RawValue / TotalDelegated.RawValue;

        public FungibleAssetValue FAVToUnbond(BigInteger share)
            => TotalShares == share
                ? TotalDelegated
                : (TotalDelegated * share).DivRem(TotalShares, out _);

        BigInteger IDelegatee.Bond(
            IDelegator delegator, FungibleAssetValue fav, long height)
            => Bond((T)delegator, fav, height);

        FungibleAssetValue IDelegatee.Unbond(
            IDelegator delegator, BigInteger share, long height)
            => Unbond((T)delegator, share, height);

        void IDelegatee.Reward(IDelegator delegator, long height)
            => Reward((T)delegator, height);

        public BigInteger Bond(T delegator, FungibleAssetValue fav, long height)
        {
            CannotMutateReleationsWithoutRepository(delegator);
            Reward(delegator, height);

            if (!fav.Currency.Equals(Currency))
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

            return share;
        }

        public FungibleAssetValue Unbond(T delegator, BigInteger share, long height)
        {
            CannotMutateReleationsWithoutRepository(delegator);
            Reward(delegator, height);
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
            
            return fav;
        }

        public void Reward(T delegator, long height)
        {
            CannotMutateReleationsWithoutRepository(delegator);
            BigInteger share = _repository!.GetBond(this, delegator.Address).Share;
            IEnumerable<LumpSumRewardsRecord> lumpSumRewardsRecords =
                GetLumpSumRewardsRecords(delegator.LastRewardHeight);
            FungibleAssetValue reward = CalculateReward(share, lumpSumRewardsRecords);
            if (reward.Sign <= 0)
            {
                return;
            }

            _repository.TransferAsset(RewardPoolAddress, delegator.Address, reward);
            StartNewRewardPeriod(height);
            delegator.UpdateLastRewardHeight(height);
        }

        public Address BondAddress(Address delegatorAddress)
            => DeriveAddress(BondId, delegatorAddress);

        public Address UnbondLockInAddress(Address delegatorAddress)
            => DeriveAddress(UnbondLockInId, delegatorAddress);

        public Address RebondGraceAddress(Address delegatorAddress)
            => DeriveAddress(RebondGraceId, delegatorAddress);

        public Address CurrentLumpSumRewardsRecordAddress()
            => DeriveAddress(LumpSumRewardsRecordId);

        public Address LumpSumRewardsRecordAddress(long height)
            => DeriveAddress(LumpSumRewardsRecordId, BitConverter.GetBytes(height));


        public override bool Equals(object? obj)
            => obj is IDelegatee other && Equals(other);

        public bool Equals(IDelegatee? other)
            => ReferenceEquals(this, other)
            || (other is Delegatee<T, TSelf> delegatee
            && (GetType() != delegatee.GetType())
            && Address.Equals(delegatee.Address)
            && Currency.Equals(delegatee.Currency)
            && DelegationPoolAddress.Equals(delegatee.DelegationPoolAddress)
            && UnbondingPeriod == delegatee.UnbondingPeriod
            && RewardPoolAddress.Equals(delegatee.RewardPoolAddress)
            && Delegators.SequenceEqual(delegatee.Delegators)
            && TotalDelegated.Equals(delegatee.TotalDelegated)
            && TotalShares.Equals(delegatee.TotalShares)
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
                _repository.SetLumpSumRewardsRecord(
                    lastRecord.MoveAddress(
                        CurrentLumpSumRewardsRecordAddress()));
                lastStartHeight = lastRecord.StartHeight;
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

        private void CannotMutateReleationsWithoutRepository(T delegator)
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
