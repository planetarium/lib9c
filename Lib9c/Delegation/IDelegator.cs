using System.Collections.Immutable;
using System.Numerics;
using Bencodex;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public interface IDelegator : IBencodable
    {
        Address Address { get; }

        ImmutableSortedSet<Address> Delegatees { get; }

        void Delegate(
            IDelegatee delegatee,
            FungibleAssetValue fav,
            Delegation delegation);

        void Undelegate(
            IDelegatee delegatee,
            BigInteger share,
            long height,
            Delegation delegation);

        void Redelegate(
            IDelegatee srcDelegatee,
            IDelegatee dstDelegatee,
            BigInteger share,
            long height,
            Delegation srcDelegation,
            Delegation dstDelegation);

        void Claim(IDelegatee delegatee);
    }
}
