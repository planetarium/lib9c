using System.Collections.Immutable;
using Bencodex;
using Libplanet.Crypto;

namespace Lib9c.Delegation
{
    public interface IDelegatorMetadata : IBencodable
    {
        Address DelegatorAddress { get; }

        Address DelegatorAccountAddress { get; }

        Address Address { get; }

        Address DelegationPoolAddress { get; }

        ImmutableSortedSet<Address> Delegatees { get; }

        public void AddDelegatee(Address delegatee);

        public void RemoveDelegatee(Address delegatee);
    }
}
