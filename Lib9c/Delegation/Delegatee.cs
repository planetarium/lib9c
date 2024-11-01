#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegatee<TRepository, TDelegatee, TDelegator>
        : IDelegatee
        where TRepository : DelegationRepository<TRepository, TDelegatee, TDelegator>
        where TDelegatee : Delegatee<TRepository, TDelegatee, TDelegator>
        where TDelegator : Delegator<TRepository, TDelegatee, TDelegator>
    {
        public Delegatee(
            Address address,
            Address accountAddress,
            Currency delegationCurrency,
            Currency rewardCurrency,
            Address delegationPoolAddress,
            Address rewardPoolAddress,
            Address rewardRemainderPoolAddress,
            Address slashedPoolAddress,
            long unbondingPeriod,
            int maxUnbondLockInEntries,
            int maxRebondGraceEntries,
            TRepository repository)
            : this(
                  new DelegateeMetadata(
                      address,
                      accountAddress,
                      delegationCurrency,
                      rewardCurrency,
                      delegationPoolAddress,
                      rewardPoolAddress,
                      rewardRemainderPoolAddress,
                      slashedPoolAddress,
                      unbondingPeriod,
                      maxUnbondLockInEntries,
                      maxRebondGraceEntries),
                  repository)
        {
        }

        public Delegatee(
            Address address,
            TRepository repository)
            : this(repository.GetDelegateeMetadata(address), repository)
        {
        }

        private Delegatee(DelegateeMetadata metadata, TRepository repository)
        {
            Metadata = metadata;
            Repository = repository;
        }

        public event EventHandler<DelegationChangedEventArgs>? DelegationChanged;

        public event EventHandler? Enjailed;

        public event EventHandler? Unjailed;

        public DelegateeMetadata Metadata { get; }

        public TRepository Repository { get; }

        public Address Address => Metadata.DelegateeAddress;

        public Address AccountAddress => Metadata.DelegateeAccountAddress;

        public Address MetadataAddress => Metadata.Address;

        public Currency DelegationCurrency => Metadata.DelegationCurrency;

        public Currency RewardCurrency => Metadata.RewardCurrency;

        public Address DelegationPoolAddress => Metadata.DelegationPoolAddress;

        public Address RewardPoolAddress => Metadata.RewardPoolAddress;

        public Address RewardRemainderPoolAddress => Metadata.RewardRemainderPoolAddress;

        public Address SlashedPoolAddress => Metadata.SlashedPoolAddress;

        public long UnbondingPeriod => Metadata.UnbondingPeriod;

        public int MaxUnbondLockInEntries => Metadata.MaxUnbondLockInEntries;

        public int MaxRebondGraceEntries => Metadata.MaxRebondGraceEntries;

        public ImmutableSortedSet<Address> Delegators => Metadata.Delegators;

        public FungibleAssetValue TotalDelegated => Metadata.TotalDelegatedFAV;

        public BigInteger TotalShares => Metadata.TotalShares;

        public bool Jailed => Metadata.Jailed;

        public long JailedUntil => Metadata.JailedUntil;

        public bool Tombstoned => Metadata.Tombstoned;

        public List MetadataBencoded => Metadata.Bencoded;

        public BigInteger ShareFromFAV(FungibleAssetValue fav)
            => Metadata.ShareFromFAV(fav);

        public FungibleAssetValue FAVFromShare(BigInteger share)
            => Metadata.FAVFromShare(share);

        public BigInteger Bond(IDelegator delegator, FungibleAssetValue fav, long height)
            => Bond((TDelegator)delegator, fav, height);

        public FungibleAssetValue Unbond(IDelegator delegator, BigInteger share, long height)
            => Unbond((TDelegator)delegator, share, height);

        public void DistributeReward(IDelegator delegator, long height)
            => DistributeReward((TDelegator)delegator, height);

        public void Jail(long releaseHeight)
        {
            Metadata.JailedUntil = releaseHeight;
            Metadata.Jailed = true;
            Repository.SetDelegateeMetadata(Metadata);
            OnEnjailed(EventArgs.Empty);
        }

        public void Unjail(long height)
        {
            if (!Jailed)
            {
                throw new InvalidOperationException("Cannot unjail non-jailed delegatee.");
            }

            if (Tombstoned)
            {
                throw new InvalidOperationException("Cannot unjail tombstoned delegatee.");
            }

            if (JailedUntil >= height)
            {
                throw new InvalidOperationException("Cannot unjail before jailed until.");
            }

            Metadata.JailedUntil = -1L;
            Metadata.Jailed = false;
            Repository.SetDelegateeMetadata(Metadata);
            OnUnjailed(EventArgs.Empty);
        }

        public void Tombstone()
        {
            Jail(long.MaxValue);
            Metadata.Tombstoned = true;
            Repository.SetDelegateeMetadata(Metadata);
        }

        public Address BondAddress(Address delegatorAddress)
            => Metadata.BondAddress(delegatorAddress);

        public Address UnbondLockInAddress(Address delegatorAddress)
            => Metadata.UnbondLockInAddress(delegatorAddress);

        public Address RebondGraceAddress(Address delegatorAddress)
            => Metadata.RebondGraceAddress(delegatorAddress);

        public Address CurrentLumpSumRewardsRecordAddress()
            => Metadata.CurrentLumpSumRewardsRecordAddress();

        public Address LumpSumRewardsRecordAddress(long height)
            => Metadata.LumpSumRewardsRecordAddress(height);

        public virtual BigInteger Bond(TDelegator delegator, FungibleAssetValue fav, long height)
        {
            DistributeReward(delegator, height);

            if (!fav.Currency.Equals(DelegationCurrency))
            {
                throw new InvalidOperationException(
                    "Cannot bond with invalid currency.");
            }

            if (Tombstoned)
            {
                throw new InvalidOperationException(
                    "Cannot bond to tombstoned delegatee.");
            }

            Bond bond = Repository.GetBond((TDelegatee)this, delegator.Address);
            BigInteger share = ShareFromFAV(fav);
            bond = bond.AddShare(share);
            Metadata.AddDelegator(delegator.Address);
            Metadata.AddShare(share);
            Metadata.AddDelegatedFAV(fav);
            Repository.SetBond(bond);
            StartNewRewardPeriod(height);
            Repository.SetDelegateeMetadata(Metadata);
            OnDelegationChanged(new DelegationChangedEventArgs(height));

            return share;
        }

        public FungibleAssetValue Unbond(TDelegator delegator, BigInteger share, long height)
        {
            DistributeReward(delegator, height);
            if (TotalShares.IsZero || TotalDelegated.RawValue.IsZero)
            {
                throw new InvalidOperationException(
                    "Cannot unbond without bonding.");
            }

            Bond bond = Repository!.GetBond((TDelegatee)this, delegator.Address);
            FungibleAssetValue fav = FAVFromShare(share);
            bond = bond.SubtractShare(share);
            if (bond.Share.IsZero)
            {
                bond = bond.ClearLastDistributeHeight();
                Metadata.RemoveDelegator(delegator.Address);
            }

            Metadata.RemoveShare(share);
            Metadata.RemoveDelegatedFAV(fav);
            Repository.SetBond(bond);
            StartNewRewardPeriod(height);
            Repository.SetDelegateeMetadata(Metadata);
            OnDelegationChanged(new DelegationChangedEventArgs(height));

            return fav;
        }

        public void DistributeReward(TDelegator delegator, long height)
        {
            Bond bond = Repository.GetBond((TDelegatee)this, delegator.Address);
            BigInteger share = bond.Share;

            if (!share.IsZero && bond.LastDistributeHeight.HasValue)
            {
                IEnumerable<LumpSumRewardsRecord> lumpSumRewardsRecords =
                    GetLumpSumRewardsRecords(bond.LastDistributeHeight);

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
                        throw new ArgumentException("Fetched wrong lump sum reward record.");
                    }

                    if (!record.Delegators.Contains(delegator.Address))
                    {
                        continue;
                    }

                    FungibleAssetValue reward = record.RewardsDuringPeriod(share);
                    if (reward.Sign > 0)
                    {
                        Repository.TransferAsset(record.Address, delegator.RewardAddress, reward);
                    }

                    LumpSumRewardsRecord newRecord = record.RemoveDelegator(delegator.Address);

                    if (newRecord.Delegators.IsEmpty)
                    {
                        FungibleAssetValue remainder = Repository.GetBalance(newRecord.Address, RewardCurrency);

                        if (remainder.Sign > 0)
                        {
                            Repository.TransferAsset(newRecord.Address, RewardRemainderPoolAddress, remainder);
                        }
                    }

                    Repository.SetLumpSumRewardsRecord(newRecord);

                    linkedStartHeight = newRecord.LastStartHeight;

                    if (linkedStartHeight == -1)
                    {
                        break;
                    }
                }
            }

            if (bond.LastDistributeHeight != height)
            {
                bond = bond.UpdateLastDistributeHeight(height);
            }

            Repository.SetBond(bond);
        }

        public void CollectRewards(long height)
        {
            FungibleAssetValue rewards = Repository.GetBalance(RewardPoolAddress, RewardCurrency);
            LumpSumRewardsRecord record = Repository.GetCurrentLumpSumRewardsRecord((TDelegatee)this)
                ?? new LumpSumRewardsRecord(
                    CurrentLumpSumRewardsRecordAddress(),
                    height,
                    TotalShares,
                    Delegators,
                    RewardCurrency);
            record = record.AddLumpSumRewards(rewards);

            if (rewards.Sign > 0)
            {
                Repository.TransferAsset(RewardPoolAddress, record.Address, rewards);
            }

            Repository.SetLumpSumRewardsRecord(record);
        }

        public void Slash(BigInteger slashFactor, long infractionHeight, long height)
        {
            FungibleAssetValue slashed = TotalDelegated.DivRem(slashFactor, out var rem);
            if (rem.Sign > 0)
            {
                slashed += FungibleAssetValue.FromRawValue(rem.Currency, 1);
            }

            if (slashed > Metadata.TotalDelegatedFAV)
            {
                slashed = Metadata.TotalDelegatedFAV;
            }

            Metadata.RemoveDelegatedFAV(slashed);

            foreach (var item in Metadata.UnbondingRefs)
            {
                var unbonding = UnbondingFactory.GetUnbondingFromRef(item, Repository);

                unbonding = unbonding.Slash(slashFactor, infractionHeight, height, out var slashedFAV);

                if (slashedFAV.HasValue)
                {
                    slashed += slashedFAV.Value;
                }

                if (unbonding.IsEmpty)
                {
                    Metadata.RemoveUnbondingRef(item);
                }

                switch (unbonding)
                {
                    case UnbondLockIn unbondLockIn:
                        Repository.SetUnbondLockIn(unbondLockIn);
                        break;
                    case RebondGrace rebondGrace:
                        Repository.SetRebondGrace(rebondGrace);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid unbonding type.");
                }
            }

            Repository.TransferAsset(DelegationPoolAddress, SlashedPoolAddress, slashed);
            Repository.SetDelegateeMetadata(Metadata);
            OnDelegationChanged(new DelegationChangedEventArgs(height));
        }

        BigInteger IDelegatee.Bond(Address delegatorAddress, FungibleAssetValue fav, long height)
            => Bond(Repository.GetDelegator(delegatorAddress), fav, height);

        FungibleAssetValue IDelegatee.Unbond(Address delegatorAddress, BigInteger share, long height)
            => Unbond(Repository.GetDelegator(delegatorAddress), share, height);

        void IDelegatee.DistributeReward(Address delegatorAddress, long height)
            => DistributeReward(Repository.GetDelegator(delegatorAddress), height);

        void IDelegatee.Slash(BigInteger slashFactor, long infractionHeight, long height)
            => Slash(slashFactor, infractionHeight, height);

        public void AddUnbondingRef(UnbondingRef reference)
            => Metadata.AddUnbondingRef(reference);

        public void RemoveUnbondingRef(UnbondingRef reference)
            => Metadata.RemoveUnbondingRef(reference);

        protected virtual void OnDelegationChanged(DelegationChangedEventArgs e)
        {
            DelegationChanged?.Invoke(this, e);
        }

        protected virtual void OnEnjailed(EventArgs e)
        {
            Enjailed?.Invoke(this, e);
        }

        protected virtual void OnUnjailed(EventArgs e)
        {
            Unjailed?.Invoke(this, e);
        }

        private void StartNewRewardPeriod(long height)
        {
            LumpSumRewardsRecord? currentRecord = Repository.GetCurrentLumpSumRewardsRecord((TDelegatee)this);
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
                        Delegators,
                        RewardCurrency,
                        currentRecord.LastStartHeight);

                    Repository.SetLumpSumRewardsRecord(currentRecord);
                    return;
                }

                Address archiveAddress = LumpSumRewardsRecordAddress(lastRecord.StartHeight);
                FungibleAssetValue reward = Repository.GetBalance(lastRecord.Address, RewardCurrency);
                if (reward.Sign > 0)
                {
                    Repository.TransferAsset(lastRecord.Address, archiveAddress, reward);
                }

                lastRecord = lastRecord.MoveAddress(archiveAddress);
                Repository.SetLumpSumRewardsRecord(lastRecord);
            }

            LumpSumRewardsRecord newRecord = new(
                CurrentLumpSumRewardsRecordAddress(),
                height,
                TotalShares,
                Delegators,
                RewardCurrency,
                lastStartHeight);

            Repository.SetLumpSumRewardsRecord(newRecord);
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
            List<LumpSumRewardsRecord> records = new();
            if (lastRewardHeight is null
                || !(Repository.GetCurrentLumpSumRewardsRecord((TDelegatee)this) is LumpSumRewardsRecord record))
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

                record = Repository.GetLumpSumRewardsRecord((TDelegatee)this, lastStartHeight)
                    ?? throw new InvalidOperationException(
                        $"Lump sum rewards record for #{lastStartHeight} is missing");
            }

            return records;
        }
    }
}
