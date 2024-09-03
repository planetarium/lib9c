#nullable enable
using System;
using System.Collections.Immutable;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Nekoyume.Delegation
{
    public class DelegatorMetadata : IDelegatorMetadata
    {
        private Address? _address;

        public DelegatorMetadata(
            Address address,
            Address accountAddress,
            Address delegationPoolAddress)
            : this(
                  address,
                  accountAddress,
                  delegationPoolAddress,
                  ImmutableSortedSet<Address>.Empty)
        {
        }

        public DelegatorMetadata(
            Address address,
            Address accountAddress,
            IValue bencoded)
            : this(address, accountAddress, (List)bencoded)
        {
        }

        public DelegatorMetadata(
            Address address,
            Address accountAddress,
            List bencoded)
            : this(
                address,
                accountAddress,
                new Address(bencoded[0]),
                ((List)bencoded[1]).Select(item => new Address(item)).ToImmutableSortedSet())
        {
        }

        private DelegatorMetadata(
            Address address,
            Address accountAddress,
            Address delegationPoolAddress,
            ImmutableSortedSet<Address> delegatees)
        {
            DelegatorAddress = address;
            DelegatorAccountAddress = accountAddress;
            DelegationPoolAddress = delegationPoolAddress;
            Delegatees = delegatees;
        }

        public Address DelegatorAddress { get; }

        public Address DelegatorAccountAddress { get; }

        public Address Address
            => _address ??= DelegationAddress.DelegatorMetadataAddress(
                DelegatorAddress,
                DelegatorAccountAddress);

        public Address DelegationPoolAddress { get; }

        public ImmutableSortedSet<Address> Delegatees { get; private set; }

        public List Bencoded
            => List.Empty
                .Add(DelegationPoolAddress.Bencoded)
                .Add(new List(Delegatees.Select(a => a.Bencoded)));

        IValue IBencodable.Bencoded => Bencoded;

        public void AddDelegatee(Address delegatee)
        {
            Delegatees = Delegatees.Add(delegatee);
        }

        public void RemoveDelegatee(Address delegatee)
        {
            Delegatees = Delegatees.Remove(delegatee);
        }

        public override bool Equals(object? obj)
            => obj is IDelegator other && Equals(other);

        public virtual bool Equals(IDelegator? other)
            => ReferenceEquals(this, other)
            || (other is DelegatorMetadata delegator
            && GetType() != delegator.GetType()
            && DelegatorAddress.Equals(delegator.DelegatorAddress)
            && Delegatees.SequenceEqual(delegator.Delegatees));

        public override int GetHashCode()
            => DelegatorAddress.GetHashCode();
    }
}
