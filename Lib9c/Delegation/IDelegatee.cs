#nullable enable
using System;
using System.Collections.Immutable;
using System.Numerics;
using Bencodex;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public interface IDelegatee : IBencodable, IEquatable<IDelegatee>
    {
        Address Address { get; }

        Currency Currency { get; }

        Currency RewardCurrency { get; }

        Address DelegationPoolAddress { get; }

        long UnbondingPeriod { get; }

        int MaxUnbondLockInEntries { get; }

        int MaxRebondGraceEntries { get; }

        Address RewardCollectorAddress { get; }

        Address RewardDistributorAddress { get; }

        ImmutableSortedSet<Address> Delegators { get; }

        FungibleAssetValue TotalDelegated { get; }

        BigInteger TotalShares { get; }

        BigInteger Bond(IDelegator delegator, FungibleAssetValue fav, long height);

        FungibleAssetValue Unbond(IDelegator delegator, BigInteger share, long height);

        void DistributeReward(IDelegator delegator, long height);

        void CollectRewards(long height);

        Address BondAddress(Address delegatorAddress);

        Address UnbondLockInAddress(Address delegatorAddress);

        Address RebondGraceAddress(Address delegatorAddress);

        Address CurrentLumpSumRewardsRecordAddress();

        Address LumpSumRewardsRecordAddress(long height);
    }
}
