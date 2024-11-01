#nullable enable
using System;
using System.Collections.Immutable;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public interface IDelegator
    {
        Address Address { get; }

        Address AccountAddress { get; }

        Address DelegationPoolAddress { get; }

        Address RewardAddress { get; }

        ImmutableSortedSet<Address> Delegatees { get; }

        void Delegate(
            Address delegateeAddress,
            FungibleAssetValue fav,
            long height);

        void Undelegate(
            Address delegateeAddress,
            BigInteger share,
            long height);

        void Redelegate(
            Address srcDelegateeAddress,
            Address dstDelegateeAddress,
            BigInteger share,
            long height);

        void CancelUndelegate(
            Address delegateeAddress,
            FungibleAssetValue fav,
            long height);

        void ClaimReward(
            Address delegateeAddress,
            long height);
    }
}
