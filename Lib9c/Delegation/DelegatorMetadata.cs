#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;

namespace Nekoyume.Delegation
{
    public struct DelegatorMetadata : IBencodable
    {
        private const string StateTypeName = "delegator_metadata";
        private const long StateVersion = 1;

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
                  ImmutableArray<Address>.Empty,
                  ImmutableArray<UnbondingRef>.Empty)
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
            Delegatees = delegatees.ToImmutableArray();
            UnbondingRefs = unbondingRefs.ToImmutableArray();
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
            Delegatees = delegatees.ToImmutableArray();
            UnbondingRefs = unbondingRefs.ToImmutableArray();
        }

        public Address DelegatorAddress { get; }

        public Address DelegatorAccountAddress { get; }

        public readonly Address Address
            => DelegationAddress.DelegatorMetadataAddress(
                DelegatorAddress,
                DelegatorAccountAddress);

        public Address DelegationPoolAddress { get; }

        public Address RewardAddress { get; }

        public ImmutableArray<Address> Delegatees { get; private set; }

        public ImmutableArray<UnbondingRef> UnbondingRefs { get; private set; }

        public readonly List Bencoded
            => List.Empty
                .Add(StateTypeName)
                .Add(StateVersion)
                .Add(DelegationPoolAddress.Bencoded)
                .Add(RewardAddress.Bencoded)
                .Add(new List(Delegatees.Select(a => a.Bencoded)))
                .Add(new List(UnbondingRefs.Select(unbondingRef => unbondingRef.Bencoded)));

        readonly IValue IBencodable.Bencoded => Bencoded;

        public readonly DelegatorMetadata AddDelegatee(Address delegatee)
        {
            var metadata = this;
            if (!Delegatees.Contains(delegatee))
            {
                metadata.Delegatees = Delegatees.Add(delegatee);
            }

            return metadata;
        }

        public readonly DelegatorMetadata RemoveDelegatee(Address delegatee)
        {
            var metadata = this;
            if (Delegatees.Contains(delegatee))
            {
                metadata.Delegatees = Delegatees.Remove(delegatee);
            }

            return metadata;
        }

        public readonly DelegatorMetadata AddUnbondingRef(UnbondingRef unbondingRef)
        {
            var metadata = this;
            if (!UnbondingRefs.Contains(unbondingRef))
            {
                metadata.UnbondingRefs = UnbondingRefs.Add(unbondingRef);
            }

            return metadata;
        }

        public readonly DelegatorMetadata RemoveUnbondingRef(UnbondingRef unbondingRef)
        {
            var metadata = this;
            if (UnbondingRefs.Contains(unbondingRef))
            {
                metadata.UnbondingRefs = UnbondingRefs.Remove(unbondingRef);
            }

            return metadata;
        }
    }
}
