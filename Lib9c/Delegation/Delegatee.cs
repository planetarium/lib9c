#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegatee<T, TSelf> : IDelegatee
        where T : Delegator<TSelf, T>
        where TSelf : Delegatee<T, TSelf>
    {
        private static readonly ActivitySource ActivitySource
            = new ActivitySource("Lib9c.Delegation.Delegatee");

        public Delegatee(
            Address address,
            Address accountAddress,
            Currency delegationCurrency,
            IEnumerable<Currency> rewardCurrencies,
            Address delegationPoolAddress,
            Address rewardPoolAddress,
            Address rewardRemainderPoolAddress,
            Address slashedPoolAddress,
            long unbondingPeriod,
            int maxUnbondLockInEntries,
            int maxRebondGraceEntries,
            IDelegationRepository repository)
            : this(
                  new DelegateeMetadata(
                      address,
                      accountAddress,
                      delegationCurrency,
                      rewardCurrencies,
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
            IDelegationRepository repository)
            : this(repository.GetDelegateeMetadata(address), repository)
        {
        }

        private Delegatee(DelegateeMetadata metadata, IDelegationRepository repository)
        {
            Metadata = metadata;
            Repository = repository;
        }

        public event EventHandler<long>? DelegationChanged;

        public event EventHandler? Enjailed;

        public event EventHandler? Unjailed;

        public DelegateeMetadata Metadata { get; }

        public IDelegationRepository Repository { get; }

        public Address Address => Metadata.DelegateeAddress;

        public Address AccountAddress => Metadata.DelegateeAccountAddress;

        public Address MetadataAddress => Metadata.Address;

        public Currency DelegationCurrency => Metadata.DelegationCurrency;

        public ImmutableSortedSet<Currency> RewardCurrencies => Metadata.RewardCurrencies;

        public Address DelegationPoolAddress => Metadata.DelegationPoolAddress;

        public Address RewardPoolAddress => Metadata.RewardPoolAddress;

        public Address RewardRemainderPoolAddress => Metadata.RewardRemainderPoolAddress;

        public Address SlashedPoolAddress => Metadata.SlashedPoolAddress;

        public long UnbondingPeriod => Metadata.UnbondingPeriod;

        public int MaxUnbondLockInEntries => Metadata.MaxUnbondLockInEntries;

        public int MaxRebondGraceEntries => Metadata.MaxRebondGraceEntries;

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
            => Bond((T)delegator, fav, height);

        public FungibleAssetValue Unbond(IDelegator delegator, BigInteger share, long height)
            => Unbond((T)delegator, share, height);

        public void DistributeReward(IDelegator delegator, long height)
            => DistributeReward((T)delegator, height);

        public void Jail(long releaseHeight)
        {
            Metadata.JailedUntil = releaseHeight;
            Metadata.Jailed = true;
            Repository.SetDelegateeMetadata(Metadata);
            Enjailed?.Invoke(this, EventArgs.Empty);
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
            Unjailed?.Invoke(this, EventArgs.Empty);
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

        public Address DistributionPoolAddress()
            => Metadata.DistributionPoolAddress();

        public Address CurrentRewardBaseAddress()
            => Metadata.CurrentRewardBaseAddress();

        public Address RewardBaseAddress(long height)
            => Metadata.RewardBaseAddress(height);

        public Address CurrentLumpSumRewardsRecordAddress()
            => Metadata.CurrentLumpSumRewardsRecordAddress();

        public Address LumpSumRewardsRecordAddress(long height)
            => Metadata.LumpSumRewardsRecordAddress(height);

        public virtual BigInteger Bond(T delegator, FungibleAssetValue fav, long height)
        {
            using var activity = ActivitySource.StartActivity("Bond");

            using (Activity? _ = ActivitySource.StartActivity(
                "DistributeReward",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty))
            {
                DistributeReward(delegator, height);
            }

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

            var getBondActivity = ActivitySource.StartActivity(
                "GetBond",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Bond bond = Repository.GetBond(this, delegator.Address);
            getBondActivity?.Dispose();

            var shareFromFAVActivity = ActivitySource.StartActivity(
                "ShareFromFAV",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            BigInteger share = ShareFromFAV(fav);
            shareFromFAVActivity?.Dispose();

            var addShareActivity = ActivitySource.StartActivity(
                "AddShare",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            bond = bond.AddShare(share);
            addShareActivity?.Dispose();

            var addShareMetadataActivity = ActivitySource.StartActivity(
                "AddShareMetadata",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Metadata.AddShare(share);
            addShareMetadataActivity?.Dispose();

            var addDelegatedFAVMetadataActivity = ActivitySource.StartActivity(
                "AddDelegatedFAVMetadata",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Metadata.AddDelegatedFAV(fav);
            addDelegatedFAVMetadataActivity?.Dispose();

            var setBondActivity = ActivitySource.StartActivity(
                "SetBond",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Repository.SetBond(bond);
            setBondActivity?.Dispose();

            using (Activity? _ = ActivitySource.StartActivity(
                "StartNewRewardPeriod",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty))
            {
                StartNewRewardPeriod(height);
            }

            var setDelegateeMetadataActivity = ActivitySource.StartActivity(
                "SetDelegateeMetadata",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Repository.SetDelegateeMetadata(Metadata);
            setDelegateeMetadataActivity?.Dispose();

            DelegationChanged?.Invoke(this, height);

            return share;
        }

        BigInteger IDelegatee.Bond(IDelegator delegator, FungibleAssetValue fav, long height)
            => Bond((T)delegator, fav, height);

        public FungibleAssetValue Unbond(T delegator, BigInteger share, long height)
        {
            using var activity = ActivitySource.StartActivity("Unbond");

            using (Activity? _ = ActivitySource.StartActivity(
                "DistributeReward",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty))
            {
                DistributeReward(delegator, height);
            }

            if (TotalShares.IsZero || TotalDelegated.RawValue.IsZero)
            {
                throw new InvalidOperationException(
                    "Cannot unbond without bonding.");
            }

            var getBondActivity = ActivitySource.StartActivity(
                "GetBond",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Bond bond = Repository!.GetBond(this, delegator.Address);
            getBondActivity?.Dispose();

            var favFromShareActivity = ActivitySource.StartActivity(
                "FAVFromShare",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            FungibleAssetValue fav = FAVFromShare(share);
            favFromShareActivity?.Dispose();

            var subtractShareActivity = ActivitySource.StartActivity(
                "SubtractShare",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            bond = bond.SubtractShare(share);
            if (bond.Share.IsZero)
            {
                bond = bond.ClearLastDistributeHeight();
            }
            subtractShareActivity?.Dispose();

            var removeShareMetadataActivity = ActivitySource.StartActivity(
               "RemoveShareMetadata",
               ActivityKind.Internal,
               activity?.Id ?? string.Empty);
            Metadata.RemoveShare(share);
            removeShareMetadataActivity?.Dispose();

            var removeDelegatedFAVMetadataActivity = ActivitySource.StartActivity(
                "RemoveDelegatedFAVMetadata",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Metadata.RemoveDelegatedFAV(fav);

            var setBondActivity = ActivitySource.StartActivity(
                "SetBond",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Repository.SetBond(bond);
            setBondActivity?.Dispose();

            using (Activity? _ = ActivitySource.StartActivity(
                "StartNewRewardPeriod",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty))
            {
                StartNewRewardPeriod(height);
            }

            var setDelegateeMetadataActivity = ActivitySource.StartActivity(
                "SetDelegateeMetadata",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Repository.SetDelegateeMetadata(Metadata);
            setDelegateeMetadataActivity?.Dispose();

            DelegationChanged?.Invoke(this, height);

            return fav;
        }

        FungibleAssetValue IDelegatee.Unbond(IDelegator delegator, BigInteger share, long height)
            => Unbond((T)delegator, share, height);

        public void DistributeReward(T delegator, long height)
        {
            using var activity = ActivitySource.StartActivity("DistributeReward");

            var getBondActivity = ActivitySource.StartActivity(
                "GetBond",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Bond bond = Repository.GetBond(this, delegator.Address);
            BigInteger share = bond.Share;
            getBondActivity?.Dispose();

            if (!share.IsZero && bond.LastDistributeHeight.HasValue)
            {
                if (Repository.GetCurrentRewardBase(this) is RewardBase rewardBase)
                {
                    var distributeByRewardBaseActivity = ActivitySource.StartActivity(
                        "DistributeByRewardBaseActivity",
                        ActivityKind.Internal,
                        activity?.Id ?? string.Empty);
                    var lastRewardBase = Repository.GetRewardBase(this, bond.LastDistributeHeight.Value);
                    TransferReward(delegator, share, rewardBase, lastRewardBase);
                    // TransferRemainders(newRecord);
                    distributeByRewardBaseActivity?.Dispose();
                }
                else
                {
                    var distributeByLumpSumRewardsRecordActivity = ActivitySource.StartActivity(
                        "DistributeByLumpSumRewardsRecordActivity",
                        ActivityKind.Internal,
                        activity?.Id ?? string.Empty);
                    IEnumerable<LumpSumRewardsRecord> lumpSumRewardsRecords
                        = GetLumpSumRewardsRecords(bond.LastDistributeHeight);

                    foreach (LumpSumRewardsRecord record in lumpSumRewardsRecords)
                    {
                        TransferReward(delegator, share, record);
                        // TransferRemainders(newRecord);
                        Repository.SetLumpSumRewardsRecord(record);
                    }

                    distributeByLumpSumRewardsRecordActivity?.Dispose();
                }
            }

            var updateLastDistributeHeightActivity = ActivitySource.StartActivity(
                "UpdateLastDistributeHeight",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            if (bond.LastDistributeHeight != height)
            {
                bond = bond.UpdateLastDistributeHeight(height);
            }
            updateLastDistributeHeightActivity?.Dispose();

            var setBondActivity = ActivitySource.StartActivity(
                "SetBond",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Repository.SetBond(bond);
            setBondActivity?.Dispose();
        }

        void IDelegatee.DistributeReward(IDelegator delegator, long height)
            => DistributeReward((T)delegator, height);

        public void CollectRewards(long height)
        {
            using var activity = ActivitySource.StartActivity("CollectRewards");

            var calculateRewardActivity = ActivitySource.StartActivity(
                "CalculateReward",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            var rewards = RewardCurrencies.Select(c => Repository.GetBalance(RewardPoolAddress, c));
            calculateRewardActivity?.Dispose();

            if (Repository.GetCurrentRewardBase(this) is RewardBase rewardBase)
            {
                var collectWithRewardBaseActivity = ActivitySource.StartActivity(
                    "CollectWithRewardBase",
                    ActivityKind.Internal,
                    activity?.Id ?? string.Empty);

                foreach (var rewardsEach in rewards)
                {
                    if (rewardsEach.Sign > 0)
                    {
                        Repository.TransferAsset(RewardPoolAddress, DistributionPoolAddress(), rewardsEach);
                    }
                }

                Repository.SetRewardBase(rewardBase);
                collectWithRewardBaseActivity?.Dispose();
            }
            else
            {
                var collectWithLumpSumRewardsRecordActivity = ActivitySource.StartActivity(
                    "CollectWithLumpSumRewardsRecord",
                    ActivityKind.Internal,
                    activity?.Id ?? string.Empty);
                LumpSumRewardsRecord record = Repository.GetCurrentLumpSumRewardsRecord(this)
                    ?? new LumpSumRewardsRecord(
                        CurrentLumpSumRewardsRecordAddress(),
                        height,
                        TotalShares,
                        RewardCurrencies);
                record = record.AddLumpSumRewards(rewards);

                foreach (var rewardsEach in rewards)
                {
                    if (rewardsEach.Sign > 0)
                    {
                        Repository.TransferAsset(RewardPoolAddress, record.Address, rewardsEach);
                    }
                }
            
                Repository.SetLumpSumRewardsRecord(record);
                collectWithLumpSumRewardsRecordActivity?.Dispose();
            }
        }

        public virtual void Slash(BigInteger slashFactor, long infractionHeight, long height)
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

            var delegationBalance = Repository.GetBalance(DelegationPoolAddress, DelegationCurrency);
            if (delegationBalance < slashed)
            {
                slashed = delegationBalance;
            }

            if (slashed > DelegationCurrency * 0)
            {
                Repository.TransferAsset(DelegationPoolAddress, SlashedPoolAddress, slashed);
            }

            Repository.SetDelegateeMetadata(Metadata);
            DelegationChanged?.Invoke(this, height);
        }

        void IDelegatee.Slash(BigInteger slashFactor, long infractionHeight, long height)
            => Slash(slashFactor, infractionHeight, height);

        public void AddUnbondingRef(UnbondingRef reference)
            => Metadata.AddUnbondingRef(reference);

        public void RemoveUnbondingRef(UnbondingRef reference)
            => Metadata.RemoveUnbondingRef(reference);

        public ImmutableDictionary<Currency, FungibleAssetValue> CalculateReward(
            BigInteger share,
            IEnumerable<LumpSumRewardsRecord> lumpSumRewardsRecords)
        {
            ImmutableDictionary<Currency, FungibleAssetValue> reward
                = RewardCurrencies.ToImmutableDictionary(c => c, c => c * 0);

            foreach (LumpSumRewardsRecord record in lumpSumRewardsRecords)
            {
                var rewardDuringPeriod = record.RewardsDuringPeriod(share);
                reward = rewardDuringPeriod.Aggregate(reward, (acc, pair)
                    => acc.SetItem(pair.Key, acc[pair.Key] + pair.Value));
            }

            return reward;
        }

        public void StartNewRewardPeriod(long height)
        {
            using var activity = ActivitySource.StartActivity("StartNewRewardPeriod");

            var migrateLumpSumRewardsRecordsActivity = ActivitySource.StartActivity(
                "MigrateLumpSumRewardsRecords",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            MigrateLumpSumRewardsRecords();
            migrateLumpSumRewardsRecordsActivity?.Dispose();

            var startNewRewardBaseActivity = ActivitySource.StartActivity(
                "StartNewRewardBase",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            RewardBase newRewardBase;
            if (Repository.GetCurrentRewardBase(this) is RewardBase rewardBase)
            {
                newRewardBase = rewardBase.UpdateTotalShares(TotalShares);
                if (Repository.GetRewardBase(this, height) is not null)
                {
                    Repository.SetRewardBase(newRewardBase);
                    return;
                }

                Address archiveAddress = RewardBaseAddress(height);
                var archivedRewardBase = rewardBase.AttachHeight(archiveAddress, height);
                Repository.SetRewardBase(archivedRewardBase);
            }
            else
            {
                if (TotalShares.IsZero)
                {
                    return;
                }

                newRewardBase = new(
                    CurrentRewardBaseAddress(),
                    TotalShares,
                    RewardCurrencies);
            }

            Repository.SetRewardBase(newRewardBase);
            startNewRewardBaseActivity?.Dispose();
        }

        private List<LumpSumRewardsRecord> GetLumpSumRewardsRecords(long? lastRewardHeight)
        {
            using var activity = ActivitySource.StartActivity("GetLumpSumRewardsRecords");

            var getCurrentLumpSumRewardsRecordsActivity = ActivitySource.StartActivity(
                "GetCurrentLumpSumRewardsRecords",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            List<LumpSumRewardsRecord> records = new();
            if (lastRewardHeight is null
                || !(Repository.GetCurrentLumpSumRewardsRecord(this) is LumpSumRewardsRecord record))
            {
                return records;
            }
            getCurrentLumpSumRewardsRecordsActivity?.Dispose();

            var getLumpSumRewardsRecordActivity = ActivitySource.StartActivity(
                "GetLumpSumRewardsRecord",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            while (record.StartHeight >= lastRewardHeight)
            {
                records.Add(record);

                if (!(record.LastStartHeight is long lastStartHeight))
                {
                    break;
                }

                record = Repository.GetLumpSumRewardsRecord(this, lastStartHeight)
                    ?? throw new InvalidOperationException(
                        $"Lump sum rewards record for #{lastStartHeight} is missing");
            }
            getLumpSumRewardsRecordActivity?.Dispose();

            return records;
        }

        private void TransferReward(T delegator, BigInteger share, LumpSumRewardsRecord record)
        {
            ImmutableSortedDictionary<Currency, FungibleAssetValue> reward = record.RewardsDuringPeriod(share);
            foreach (var rewardEach in reward)
            {
                if (rewardEach.Value.Sign > 0)
                {
                    Repository.TransferAsset(record.Address, delegator.RewardAddress, rewardEach.Value);
                }
            }
        }

        private void TransferReward(
            T delegator,
            BigInteger share,
            RewardBase currentRewardBase,
            RewardBase? lastRewardBase)
        {
            var currentCumulative = currentRewardBase.CumulativeRewardDuringPeriod(share);
            var lastCumulative = lastRewardBase?.CumulativeRewardDuringPeriod(share);

            foreach (var c in currentCumulative)
            {
                var lastCumulativeEach = lastCumulative?[c.Key] ?? c.Key * 0;

                if (c.Value < lastCumulativeEach)
                {
                    throw new InvalidOperationException("Invalid reward base.");
                }

                var reward = c.Value - lastCumulativeEach;

                if (reward.Sign > 0)
                {
                    Repository.TransferAsset(DistributionPoolAddress(), delegator.RewardAddress, reward);
                }
            }
        }

        private void TransferRemainders(LumpSumRewardsRecord record)
        {
            foreach (var rewardCurrency in RewardCurrencies)
            {
                FungibleAssetValue remainder = Repository.GetBalance(record.Address, rewardCurrency);

                if (remainder.Sign > 0)
                {
                    Repository.TransferAsset(record.Address, RewardRemainderPoolAddress, remainder);
                }
            }
        }

        private void MigrateLumpSumRewardsRecords()
        {
            List<LumpSumRewardsRecord> records = new();
            if (!(Repository.GetCurrentLumpSumRewardsRecord(this) is LumpSumRewardsRecord record))
            {
                return;
            }

            while (record.LastStartHeight is long lastStartHeight)
            {
                records.Add(record);
                record = Repository.GetLumpSumRewardsRecord(this, lastStartHeight)
                    ?? throw new InvalidOperationException(
                            $"Lump sum rewards record for #{lastStartHeight} is missing");
            }

            records.Reverse();

            RewardBase? rewardBase = null;
            RewardBase? newRewardBase = null;
            foreach (var recordEach in records)
            {
                if (rewardBase is null)
                {
                    rewardBase = new RewardBase(
                        CurrentRewardBaseAddress(),
                        recordEach.TotalShares,
                        recordEach.LumpSumRewards.Keys);
                }
                else
                {
                    newRewardBase = rewardBase.UpdateTotalShares(recordEach.TotalShares);
                    if (Repository.GetRewardBase(this, recordEach.StartHeight) is not null)
                    {
                        Repository.SetRewardBase(newRewardBase);
                    }
                    else
                    {
                        Address archiveAddress = RewardBaseAddress(recordEach.StartHeight);
                        var archivedRewardBase = rewardBase.AttachHeight(archiveAddress, recordEach.StartHeight);
                        Repository.SetRewardBase(archivedRewardBase);
                    }

                    rewardBase = newRewardBase;
                }

                rewardBase = rewardBase.AddRewards(recordEach.LumpSumRewards.Values);
                foreach (var r in recordEach.LumpSumRewards)
                {
                    var toTransfer = Repository.GetBalance(recordEach.Address, r.Key);
                    if (toTransfer.Sign > 0)
                    {
                        Repository.TransferAsset(RewardPoolAddress, DistributionPoolAddress(), toTransfer);
                    }
                }

                Repository.RemoveLumpSumRewardsRecord(recordEach);
            }

            if (rewardBase is RewardBase rewardBaseToSet)
            {
                Repository.SetRewardBase(rewardBaseToSet);
            }
        }
    }
}
