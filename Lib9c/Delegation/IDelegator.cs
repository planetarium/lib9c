#nullable enable
using System;
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

        void Delegate(
            IDelegatee delegatee,
            FungibleAssetValue fav,
            long height);

        void Undelegate(
            IDelegatee delegatee,
            BigInteger share,
            long height);

        void Redelegate(
            IDelegatee srcDelegatee,
            IDelegatee dstDelegatee,
            BigInteger share,
            long height);

        void ClaimReward(
            IDelegatee delegatee,
            long height);
    }
}
