#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegatee<TRepository, TDelegatee, TDelegator> : IDelegatee
        where TRepository : DelegationRepository<TRepository, TDelegatee, TDelegator>
        where TDelegatee : Delegatee<TRepository, TDelegatee, TDelegator>
        where TDelegator : Delegator<TRepository, TDelegatee, TDelegator>
    {
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
            TRepository repository)
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
            TRepository repository)
            : this(repository.GetDelegateeMetadata(address), repository)
        {
        }

        private Delegatee(DelegateeMetadata metadata, TRepository repository)
        {
            Metadata = metadata;
            Repository = repository;
        }

        public DelegateeMetadata Metadata { get; }

        public TRepository Repository { get; }

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

        BigInteger IDelegatee.Bond(IDelegator delegator, FungibleAssetValue fav, long height)
            => Bond((TDelegator)delegator, fav, height);

        FungibleAssetValue IDelegatee.Unbond(IDelegator delegator, BigInteger share, long height)
            => Unbond((TDelegator)delegator, share, height);

        void IDelegatee.DistributeReward(IDelegator delegator, long height)
            => DistributeReward((TDelegator)delegator, height);

        public void Jail(long releaseHeight)
        {
            Metadata.JailedUntil = releaseHeight;
            Metadata.Jailed = true;
            Repository.SetDelegateeMetadata(Metadata);
            OnEnjailed();
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
            OnUnjailed();
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

        /// <summary>
        /// Get the <see cref="Address"/> of the distribution pool
        /// where the rewards are distributed from.
        /// </summary>
        /// <returns>
        /// <see cref="Address"/> of the distribution pool.
        /// </returns>
        public Address DistributionPoolAddress()
            => Metadata.DistributionPoolAddress();

        /// <summary>
        /// Get the <see cref="Address"/> of the current <see cref="RewardBase"/>.
        /// </summary>
        /// <returns>
        /// <see cref="Address"/> of the current <see cref="RewardBase"/>.
        /// </returns>
        public Address CurrentRewardBaseAddress()
            => Metadata.CurrentRewardBaseAddress();

        /// <summary>
        /// Get the <see cref="Address"/> of the <see cref="RewardBase"/> at the given height.
        /// </summary>
        /// <param name="height"></param>
        /// <returns>
        /// <see cref="Address"/> of the <see cref="RewardBase"/> at the given height.
        /// </returns>
        public Address RewardBaseAddress(long height)
            => Metadata.RewardBaseAddress(height);

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
            Metadata.AddShare(share);
            Metadata.AddDelegatedFAV(fav);
            Repository.SetBond(bond);
            StartNewRewardPeriod(height);
            Repository.SetDelegateeMetadata(Metadata);
            OnDelegationChanged(height);

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
            }

            Metadata.RemoveShare(share);
            Metadata.RemoveDelegatedFAV(fav);
            Repository.SetBond(bond);
            StartNewRewardPeriod(height);
            Repository.SetDelegateeMetadata(Metadata);
            OnDelegationChanged(height);

            return fav;
        }

        public void DistributeReward(TDelegator delegator, long height)
        {
            Bond bond = Repository.GetBond((TDelegatee)this, delegator.Address);
            BigInteger share = bond.Share;

            if (!share.IsZero && bond.LastDistributeHeight.HasValue)
            {
                if (Repository.GetCurrentRewardBase((TDelegatee)this) is RewardBase rewardBase)
                {
                    var lastRewardBase = Repository.GetRewardBase((TDelegatee)this, bond.LastDistributeHeight.Value);
                    var rewards = CalculateRewards(share, rewardBase, lastRewardBase);
                    TransferRewards(delegator, rewards);
                    // TransferRemainders(newRecord);
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
            var rewards = RewardCurrencies.Select(c => Repository.GetBalance(RewardPoolAddress, c));
            if (Repository.GetCurrentRewardBase((TDelegatee)this) is RewardBase rewardBase)
            {
                rewardBase = rewardBase.AddRewards(rewards, TotalShares);

                foreach (var rewardsEach in rewards)
                {
                    if (rewardsEach.Sign > 0)
                    {
                        Repository.TransferAsset(RewardPoolAddress, DistributionPoolAddress(), rewardsEach);
                    }
                }

                Repository.SetRewardBase(rewardBase);
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

                unbonding = unbonding.Slash(slashFactor, infractionHeight, height, SlashedPoolAddress);

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
            OnDelegationChanged(height);
        }

        void IDelegatee.Slash(BigInteger slashFactor, long infractionHeight, long height)
            => Slash(slashFactor, infractionHeight, height);

        /// <summary>
        /// Start a new reward period.
        /// It generates a new <see cref="RewardBase"/> and archives the current one.
        /// </summary>
        /// <param name="height">
        /// The height of the block where the new reward period starts.
        /// </param>
        public void StartNewRewardPeriod(long height)
        {
            RewardBase newRewardBase;
            if (Repository.GetCurrentRewardBase((TDelegatee)this) is RewardBase rewardBase)
            {
                newRewardBase = rewardBase.UpdateSigFig(TotalShares);
                if (Repository.GetRewardBase((TDelegatee)this, height) is not null)
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
        }

        public IEnumerable<FungibleAssetValue> CalculateRewards(
            BigInteger share,
            RewardBase currentRewardBase,
            RewardBase? lastRewardBase)
        {
            var currentCumulative = currentRewardBase.CumulativeRewardDuringPeriod(share);
            var lastCumulative = lastRewardBase?.CumulativeRewardDuringPeriod(share)
                ?? ImmutableSortedDictionary<Currency, FungibleAssetValue>.Empty;

            foreach (var c in currentCumulative)
            {
                var lastCumulativeEach = lastCumulative.GetValueOrDefault(c.Key, defaultValue: c.Key * 0);

                if (c.Value < lastCumulativeEach)
                {
                    throw new InvalidOperationException("Invalid reward base.");
                }

                var reward = c.Value - lastCumulativeEach;
                yield return reward;
            }
        }

        protected virtual void OnDelegationChanged(long height)
        {
        }

        protected virtual void OnEnjailed()
        {
        }

        protected virtual void OnUnjailed()
        {
        }

        internal void AddUnbondingRef(UnbondingRef reference)
            => Metadata.AddUnbondingRef(reference);

        internal void RemoveUnbondingRef(UnbondingRef reference)
            => Metadata.RemoveUnbondingRef(reference);

        private void TransferRewards(TDelegator delegator, IEnumerable<FungibleAssetValue> rewards)
        {
            foreach (var reward in rewards)
            {
                if (reward.Sign > 0)
                {
                    Repository.TransferAsset(DistributionPoolAddress(), delegator.RewardAddress, reward);
                }
            }
        }
    }
}
