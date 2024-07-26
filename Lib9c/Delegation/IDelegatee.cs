using System.Collections.Immutable;
using System.Numerics;
using Bencodex;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public interface IDelegatee<T> : IBencodable
        where T : IDelegator
    {
        Address Address { get; }

        Currency Currency { get; }

        Address PoolAddress { get; }

        long UnbondingPeriod { get; }

        ImmutableSortedSet<Address> Delegators { get; }

        FungibleAssetValue TotalDelegated { get; }

        BigInteger TotalShares { get; }

        Delegation Bond(T delegator, FungibleAssetValue fav, Delegation delegation);

        Delegation Unbond(T delegator, BigInteger share , Delegation delegation);

        Address DelegationAddress(Address delegatorAddress);

        // TODO: Add a method to claim rewards.cd 
    }
}
