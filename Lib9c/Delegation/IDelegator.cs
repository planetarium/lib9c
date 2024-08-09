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
    public interface IDelegator : IBencodable, IEquatable<IDelegator>
    {
        Address Address { get; }

        ImmutableSortedSet<Address> Delegatees { get; }

        IDelegateResult Delegate(
            IDelegatee delegatee,
            FungibleAssetValue fav,
            long height,
            Bond bond);

        IUndelegateResult Undelegate(
            IDelegatee delegatee,
            BigInteger share,
            long height,
            Bond bond,
            UnbondLockIn unbondLockIn,
            UnbondingSet unbondingSet);

        IRedelegateResult Redelegate(
            IDelegatee srcDelegatee,
            IDelegatee dstDelegatee,
            BigInteger share,
            long height,
            Bond srcBond,
            Bond dstBond,
            RebondGrace srcRebondGrace,
            UnbondingSet unbondingSet);

        IClaimRewardResult ClaimReward(
            IDelegatee delegatee,
            IEnumerable<LumpSumRewardsRecord> lumpSumRewardRecords,
            Bond bond,
            long height);
    }
}
