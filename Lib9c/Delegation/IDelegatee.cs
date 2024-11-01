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

        Currency RewardCurrency { get; }

        Address DelegationPoolAddress { get; }

        Address RewardRemainderPoolAddress { get; }

        long UnbondingPeriod { get; }

        int MaxUnbondLockInEntries { get; }

        int MaxRebondGraceEntries { get; }

        Address RewardPoolAddress { get; }

        ImmutableSortedSet<Address> Delegators { get; }

        FungibleAssetValue TotalDelegated { get; }

        BigInteger TotalShares { get; }

        bool Jailed { get; }

        long JailedUntil { get; }

        bool Tombstoned { get; }

        BigInteger ShareFromFAV(FungibleAssetValue fav);

        FungibleAssetValue FAVFromShare(BigInteger share);

        BigInteger Bond(Address delegatorAddress, FungibleAssetValue fav, long height);

        FungibleAssetValue Unbond(Address delegatorAddress, BigInteger share, long height);

        void DistributeReward(Address delegatorAddress, long height);

        void CollectRewards(long height);

        void Slash(BigInteger slashFactor, long infractionHeight, long height);

        void Jail(long releaseHeight);

        void Unjail(long height);

        void Tombstone();

        Address BondAddress(Address delegatorAddress);

        Address UnbondLockInAddress(Address delegatorAddress);

        Address RebondGraceAddress(Address delegatorAddress);

        Address CurrentLumpSumRewardsRecordAddress();

        Address LumpSumRewardsRecordAddress(long height);

        event EventHandler<DelegationChangedEventArgs>? DelegationChanged;
    }
}
