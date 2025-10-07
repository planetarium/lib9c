#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Lib9c.Action;
using Libplanet.Crypto;

namespace Lib9c.Delegation
{
    public class DelegatorMetadata : IDelegatorMetadata
    {
        private const string StateTypeName = "delegator_metadata";
        private const long StateVersion = 1;

        private Address? _address;

        public DelegatorMetadata(
            Address address,
            Address accountAddress,
            Address delegationPoolAddress,
            Address rewardAddress)
            : this(
                  address,
                  accountAddress,
                  delegationPoolAddress,
                  rewardAddress,
                  ImmutableSortedSet<Address>.Empty,
                  ImmutableSortedSet<UnbondingRef>.Empty)
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
            Address delegatorAddress,
            Address delegatorAccountAddress,
            List bencoded)
        {
            Address delegationPoolAddress;
            Address rewardAddress;
            IEnumerable<Address> delegatees;
            IEnumerable<UnbondingRef> unbondingRefs;

            // TODO: Remove this if block after migration to state version 1 is done.
            if (bencoded[0] is not Text)
            {
                // Assume state version 0
                delegationPoolAddress = new Address(bencoded[0]);
                rewardAddress = new Address(bencoded[1]);
                delegatees = ((List)bencoded[2]).Select(item => new Address(item));
                unbondingRefs = ImmutableSortedSet<UnbondingRef>.Empty;
            }
            else
            {
                if (bencoded[0] is not Text text || text != StateTypeName || bencoded[1] is not Integer integer)
                {
                    throw new InvalidCastException();
                }

                if (integer > StateVersion)
                {
                    throw new FailedLoadStateException("Un-deserializable state.");
                }

                delegationPoolAddress = new Address(bencoded[2]);
                rewardAddress = new Address(bencoded[3]);
                delegatees = ((List)bencoded[4]).Select(item => new Address(item));
                unbondingRefs = ((List)bencoded[5]).Select(item => new UnbondingRef(item));
            }

            DelegatorAddress = delegatorAddress;
            DelegatorAccountAddress = delegatorAccountAddress;
            DelegationPoolAddress = delegationPoolAddress;
            RewardAddress = rewardAddress;
            Delegatees = delegatees.ToImmutableSortedSet();
            UnbondingRefs = unbondingRefs.ToImmutableSortedSet();
        }

        private DelegatorMetadata(
            Address address,
            Address accountAddress,
            Address delegationPoolAddress,
            Address rewardAddress,
            IEnumerable<Address> delegatees,
            IEnumerable<UnbondingRef> unbondingRefs)
        {
            DelegatorAddress = address;
            DelegatorAccountAddress = accountAddress;
            DelegationPoolAddress = delegationPoolAddress;
            RewardAddress = rewardAddress;
            Delegatees = delegatees.ToImmutableSortedSet();
            UnbondingRefs = unbondingRefs.ToImmutableSortedSet();
        }

        public Address DelegatorAddress { get; }

        public Address DelegatorAccountAddress { get; }

        public Address Address
            => _address ??= DelegationAddress.DelegatorMetadataAddress(
                DelegatorAddress,
                DelegatorAccountAddress);

        public Address DelegationPoolAddress { get; }

        public Address RewardAddress { get; }

        public ImmutableSortedSet<Address> Delegatees { get; private set; }

        public ImmutableSortedSet<UnbondingRef> UnbondingRefs { get; private set; }

        public List Bencoded
            => List.Empty
                .Add(StateTypeName)
                .Add(StateVersion)
                .Add(DelegationPoolAddress.Bencoded)
                .Add(RewardAddress.Bencoded)
                .Add(new List(Delegatees.Select(a => a.Bencoded)))
                .Add(new List(UnbondingRefs.Select(unbondingRef => unbondingRef.Bencoded)));

        IValue IBencodable.Bencoded => Bencoded;

        public void AddDelegatee(Address delegatee)
        {
            Delegatees = Delegatees.Add(delegatee);
        }

        public void RemoveDelegatee(Address delegatee)
        {
            Delegatees = Delegatees.Remove(delegatee);
        }

        public void AddUnbondingRef(UnbondingRef unbondingRef)
        {
            UnbondingRefs = UnbondingRefs.Add(unbondingRef);
        }

        public void RemoveUnbondingRef(UnbondingRef unbondingRef)
        {
            UnbondingRefs = UnbondingRefs.Remove(unbondingRef);
        }

        /// <inheritdoc cref="Object.Equals(object?)" />
        public override bool Equals(object? obj)
            => obj is IDelegatorMetadata other && Equals(other);

        /// <summary>
        /// Check if the given <see cref="IDelegatorMetadata"/> is equal to this instance.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IDelegatorMetadata"/> to compare with this instance.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the given <see cref="IDelegatorMetadata"/> is equal to
        /// this instance;
        /// </returns>
        public virtual bool Equals(IDelegatorMetadata? other)
            => ReferenceEquals(this, other)
            || (other is DelegatorMetadata delegator
            && GetType() == delegator.GetType()
            && DelegatorAddress.Equals(delegator.DelegatorAddress)
            && DelegatorAccountAddress.Equals(delegator.DelegatorAccountAddress)
            && DelegationPoolAddress.Equals(delegator.DelegationPoolAddress)
            && RewardAddress.Equals(delegator.RewardAddress)
            && Delegatees.SequenceEqual(delegator.Delegatees)
            && UnbondingRefs.SequenceEqual(delegator.UnbondingRefs));

        public override int GetHashCode()
            => DelegatorAddress.GetHashCode();
    }
}
