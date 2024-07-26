using Libplanet.Crypto;
using Libplanet.Types.Assets;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace Nekoyume.Delegation
{
    public interface IDelegator
    {
        Address Address { get; }

        ImmutableSortedSet<Address> Delegatees { get; }

        Delegation Delegate(
            IDelegatee<IDelegator> delegatee,
            FungibleAssetValue fav,
            Delegation delegation);

        Delegation Undelegate(
            IDelegatee<IDelegator>
            delegatee, BigInteger share,
            long height,
            Delegation delegation);

        Delegation Redelegate(
            IDelegatee<IDelegator> delegateeFrom,
            IDelegatee<IDelegator> delegateeTo,
            BigInteger share,
            Delegation delegation);
    }
}
