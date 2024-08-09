#nullable enable
using System;
using System.Collections.Generic;
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

        Address PoolAddress { get; }

        long UnbondingPeriod { get; }

        int MaxUnbondLockInEntries { get; }

        int MaxRebondGraceEntries { get; }

        Address RewardPoolAddress { get; }

        ImmutableSortedSet<Address> Delegators { get; }

        FungibleAssetValue TotalDelegated { get; }

        BigInteger TotalShares { get; }

        BondResult Bond(IDelegator delegator, FungibleAssetValue fav, long height, Bond bond);

        UnbondResult Unbond(IDelegator delegator, BigInteger share, long height, Bond bond);

        RewardResult Reward(
            BigInteger share,
            long height,
            IEnumerable<LumpSumRewardsRecord> lumpSumRewardsRecords);

        Address BondAddress(Address delegatorAddress);

        Address UnbondLockInAddress(Address delegatorAddress);

        Address RebondGraceAddress(Address delegatorAddress);

        Address LumpSumRewardsRecordAddress(long? height = null);
    }
}
