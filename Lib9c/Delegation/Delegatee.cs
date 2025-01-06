#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegatee<TRepository, TDelegatee, TDelegator>
        where TRepository : DelegationRepository<TRepository, TDelegatee, TDelegator>
        where TDelegatee : Delegatee<TRepository, TDelegatee, TDelegator>
        where TDelegator : Delegator<TRepository, TDelegatee, TDelegator>
    {
        private readonly TRepository? _repository;

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

        private Delegatee(DelegateeMetadata metadata, TRepository? repository = null)
        {
            Metadata = metadata;
            _repository = repository;
        }

        public DelegateeMetadata Metadata { get; private set; }

        public Address Address => Metadata.DelegateeAddress;

        public FungibleAssetValue TotalDelegated => Metadata.TotalDelegatedFAV;

        public BigInteger TotalShares => Metadata.TotalShares;

        protected TRepository Repository
            => _repository ?? throw new InvalidOperationException("Repository is not set.");

        public BondResult Bond(TDelegator delegator, FungibleAssetValue fav, long height)
        {
            var repository = Repository;
            var delegationCurrency = Metadata.DelegationCurrency;
            var metadata = Metadata;
            DistributeReward(delegator, height);

            if (!fav.Currency.Equals(delegationCurrency))
            {
                throw new InvalidOperationException("Cannot bond with invalid currency.");
            }

            if (metadata.Tombstoned)
            {
                throw new InvalidOperationException("Cannot bond to tombstoned delegatee.");
            }

            var oldBond = repository.GetBond((TDelegatee)this, delegator.Address);
            var share = metadata.ShareFromFAV(fav);
            var newBond = oldBond.AddShare(share);
            metadata = metadata.AddShare(share)
                .AddDelegatedFAV(fav);

            UpdateMetadata(metadata);
            repository.SetBond(newBond);
            StartNewRewardPeriod(height);
            repository.SetDelegatee((TDelegatee)this);
            OnDelegationChanged(height);

            return new BondResult(share, newBond);
        }

        public UnbondResult Unbond(TDelegator delegator, BigInteger share, long height)
        {
            var repository = Repository;
            var metadata = Metadata;
            DistributeReward(delegator, height);
            if (metadata.TotalShares.IsZero || metadata.TotalDelegatedFAV.RawValue.IsZero)
            {
                throw new InvalidOperationException(
                    "Cannot unbond without bonding.");
            }

            var oldBond = repository.GetBond((TDelegatee)this, delegator.Address);
            var fav = metadata.FAVFromShare(share);
            var newBond = oldBond.SubtractShare(share);
            metadata = metadata.RemoveShare(share)
                .RemoveDelegatedFAV(fav);

            UpdateMetadata(metadata);
            repository.SetBond(newBond);
            StartNewRewardPeriod(height);
            repository.SetDelegatee((TDelegatee)this);
            OnDelegationChanged(height);

            return new UnbondResult(fav, newBond);
        }

        public void DistributeReward(TDelegator delegator, long height)
        {
            var repository = Repository;
            var bond = repository.GetBond((TDelegatee)this, delegator.Address);
            var share = bond.Share;

            if (!share.IsZero && bond.LastDistributeHeight is { } lastDistributeHeight)
            {
                if (repository.GetCurrentRewardBase((TDelegatee)this) is RewardBase rewardBase)
                {
                    var lastRewardBase = repository.GetRewardBase((TDelegatee)this, lastDistributeHeight);
                    var rewards = CalculateRewards(share, rewardBase, lastRewardBase);
                    TransferRewards(delegator, rewards);
                    // TransferRemainders(newRecord);
                }
            }

            if (bond.LastDistributeHeight != height)
            {
                bond = bond.UpdateLastDistributeHeight(height);
            }

            repository.SetBond(bond);
        }

        public void CollectRewards(long height)
        {
            var repository = Repository;
            var rewardCurrencies = Metadata.RewardCurrencies;
            var totalShares = Metadata.TotalShares;
            var rewards = rewardCurrencies.Select(c => repository.GetBalance(Metadata.RewardPoolAddress, c));
            if (repository.GetCurrentRewardBase((TDelegatee)this) is RewardBase rewardBase)
            {
                var distributionPoolAddress = Metadata.DistributionPoolAddress();
                rewardBase = rewardBase.AddRewards(rewards, totalShares);

                foreach (var rewardsEach in rewards)
                {
                    if (rewardsEach.Sign > 0)
                    {
                        repository.TransferAsset(Metadata.RewardPoolAddress, distributionPoolAddress, rewardsEach);
                    }
                }

                repository.SetRewardBase(rewardBase);
            }
        }

        public virtual void Slash(BigInteger slashFactor, long infractionHeight, long height)
        {
            var repository = Repository;
            var totalDelegated = Metadata.TotalDelegatedFAV;
            var delegationCurrency = Metadata.DelegationCurrency;
            FungibleAssetValue slashed = totalDelegated.DivRem(slashFactor, out var rem);
            if (rem.Sign > 0)
            {
                slashed += FungibleAssetValue.FromRawValue(rem.Currency, 1);
            }

            if (slashed > totalDelegated)
            {
                slashed = totalDelegated;
            }

            Metadata.RemoveDelegatedFAV(slashed);

            foreach (var item in Metadata.UnbondingRefs)
            {
                var unbonding = repository.GetUnbonding(item);

                unbonding = unbonding.Slash(slashFactor, infractionHeight, out var slashed1);

                FungibleAssetValue? slashedFAV = null;
                foreach (var (address, slashedEach) in slashed1)
                {
                    var delegatee = repository.GetDelegatee(address);
                    var delegator = repository.GetDelegator(unbonding.DelegatorAddress);
                    var delegateeMetadata = delegatee.Metadata;
                    delegatee.Unbond(delegator, delegateeMetadata.ShareFromFAV(slashedEach), height);
                    slashedFAV = slashedFAV.HasValue ? slashedFAV + slashedEach : slashedEach;
                }

                if (slashedFAV.HasValue)
                {
                    slashed += slashedFAV.Value;
                }

                if (unbonding.IsEmpty)
                {
                    Metadata.RemoveUnbondingRef(item);
                }

                repository.SetUnbonding(unbonding);
            }

            var delegationBalance = repository.GetBalance(Metadata.DelegationPoolAddress, delegationCurrency);
            if (delegationBalance < slashed)
            {
                slashed = delegationBalance;
            }

            if (slashed > delegationCurrency * 0)
            {
                repository.TransferAsset(Metadata.DelegationPoolAddress, Metadata.SlashedPoolAddress, slashed);
            }

            repository.SetDelegatee((TDelegatee)this);
            OnDelegationChanged(height);
        }

        /// <summary>
        /// Start a new reward period.
        /// It generates a new <see cref="RewardBase"/> and archives the current one.
        /// </summary>
        /// <param name="height">
        /// The height of the block where the new reward period starts.
        /// </param>
        public void StartNewRewardPeriod(long height)
        {
            var repository = Repository;
            var rewardCurrencies = Metadata.RewardCurrencies;
            var totalShares = Metadata.TotalShares;

            RewardBase newRewardBase;
            if (repository.GetCurrentRewardBase((TDelegatee)this) is RewardBase rewardBase)
            {
                newRewardBase = rewardBase.UpdateSigFig(totalShares);
                if (repository.GetRewardBase((TDelegatee)this, height) is not null)
                {
                    repository.SetRewardBase(newRewardBase);
                    return;
                }

                Address archiveAddress = Metadata.RewardBaseAddress(height);
                var archivedRewardBase = rewardBase.AttachHeight(archiveAddress, height);
                repository.SetRewardBase(archivedRewardBase);
            }
            else
            {
                if (totalShares.IsZero)
                {
                    return;
                }

                newRewardBase = new(
                    Metadata.CurrentRewardBaseAddress(),
                    totalShares,
                    rewardCurrencies);
            }

            repository.SetRewardBase(newRewardBase);
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

        internal void AddUnbondingRef(UnbondingRef reference)
        {
            var metaData = Metadata.AddUnbondingRef(reference);
            UpdateMetadata(metaData);
        }

        internal void RemoveUnbondingRef(UnbondingRef reference)
        {
            var metaData = Metadata.RemoveUnbondingRef(reference);
            UpdateMetadata(metaData);
        }

        protected virtual void OnDelegationChanged(long height)
        {
        }

        protected void UpdateMetadata(DelegateeMetadata metadata)
        {
            Metadata = metadata;
        }

        private void TransferRewards(TDelegator delegator, IEnumerable<FungibleAssetValue> rewards)
        {
            var repository = Repository;
            foreach (var reward in rewards)
            {
                if (reward.Sign > 0)
                {
                    repository.TransferAsset(Metadata.DistributionPoolAddress(), delegator.Metadata.RewardAddress, reward);
                }
            }
        }
    }
}
