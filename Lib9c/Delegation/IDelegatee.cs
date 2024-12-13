#nullable enable
using System;
using System.Collections.Immutable;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public interface IDelegatee
    {
        Address Address { get; }

        Address AccountAddress { get; }

        Currency DelegationCurrency { get; }

        ImmutableSortedSet<Currency> RewardCurrencies { get; }

        Address DelegationPoolAddress { get; }

        Address RewardRemainderPoolAddress { get; }

        long UnbondingPeriod { get; }

        int MaxUnbondLockInEntries { get; }

        int MaxRebondGraceEntries { get; }

        Address RewardPoolAddress { get; }

        FungibleAssetValue TotalDelegated { get; }

        BigInteger TotalShares { get; }

        bool Jailed { get; }

        long JailedUntil { get; }

        bool Tombstoned { get; }

        BigInteger ShareFromFAV(FungibleAssetValue fav);

        FungibleAssetValue FAVFromShare(BigInteger share);

        BigInteger Bond(IDelegator delegator, FungibleAssetValue fav, long height);

        FungibleAssetValue Unbond(IDelegator delegator, BigInteger share, long height);

        void DistributeReward(IDelegator delegator, long height);

        void CollectRewards(long height);

        void Slash(BigInteger slashFactor, long infractionHeight, long height);

        void Jail(long releaseHeight);

        void Unjail(long height);

        void Tombstone();

        Address BondAddress(Address delegatorAddress);

        Address UnbondLockInAddress(Address delegatorAddress);

        Address RebondGraceAddress(Address delegatorAddress);

        /// <summary>
        /// Get the <see cref="Address"/> of the current <see cref="RewardBase"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="Address"/> of the current <see cref="RewardBase"/>.
        /// </returns>
        Address CurrentRewardBaseAddress();

        /// <summary>
        /// Get the <see cref="Address"/> of the <see cref="RewardBase"/> at the given height.
        /// </summary>
        /// <param name="height">
        /// The height of the <see cref="RewardBase"/>.
        /// </param>
        /// <returns>
        /// The <see cref="Address"/> of the <see cref="RewardBase"/> at the given height.
        /// </returns>
        Address RewardBaseAddress(long height);

        Address CurrentLumpSumRewardsRecordAddress();

        Address LumpSumRewardsRecordAddress(long height);

        event EventHandler<long>? DelegationChanged;
    }
}
