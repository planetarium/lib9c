using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;

namespace Nekoyume.Action.Extensions
{
    public static class ActionBaseExtensions
    {
        public static IImmutableSet<Address> CalculateUpdateAddresses(this IEnumerable<ActionBase> actions)
        {
            IImmutableSet<Address> addresses = ImmutableHashSet<Address>.Empty;
            IActionContext rehearsalContext = new RehearsalActionContext();

            foreach (ActionBase action in actions)
            {
                try
                {
                    IWorld nextStates = action.Execute(rehearsalContext);
                    addresses = addresses.Union(nextStates.Delta.UpdatedAddresses);
                }
                catch (NotSupportedException)
                {
                    // Ignore updated addresses from incompatible actions
                }
            }

            return addresses;
        }

        private class RehearsalActionContext : IActionContext
        {
            public BlockHash? GenesisHash => default;

            public Address Signer => default;

            public TxId? TxId => default;

            public Address Miner => default;

            public long BlockIndex => default;

            public int BlockProtocolVersion => default;

            public bool Rehearsal => true;

            public IWorld PreviousState => new AddressTraceStateDelta();

            public IRandom Random => default;

            public HashDigest<SHA256>? PreviousStateRootHash => default;

            public bool BlockAction => default;

            public void UseGas(long gas)
            {
                // pass
            }

            public IActionContext GetUnconsumedContext() => null;

            public long GasUsed() => 0;

            public long GasLimit() => 0;
        }

        private class AddressTraceStateDelta : IWorld
        {
            private AddressTraceDelta _delta;

            public AddressTraceStateDelta()
                : this(new AddressTraceDelta())
            {
            }

            public AddressTraceStateDelta(AddressTraceDelta delta)
            {
                _delta = delta;
            }

            public bool Legacy { get; }

            public BlockHash? BlockHash { get; }

            public IWorldDelta Delta => _delta;

            public IWorld SetAccount(IAccount account)
            {
                return new AddressTraceStateDelta(
                    new AddressTraceDelta(Delta.UpdatedAddresses.Union(new[] { account.Address })));
            }

            public IImmutableSet<Address> UpdatedAddresses => _delta.UpdatedAddresses;

            public IAccount GetAccount(Address address)
            {
                throw new NotSupportedException();
            }

            public class AddressTraceDelta : IWorldDelta
            {
                private IImmutableSet<Address> _updatedAddresses;

                public AddressTraceDelta()
                    : this(ImmutableHashSet<Address>.Empty)
                {
                }

                public AddressTraceDelta(IImmutableSet<Address> updatedAddresses)
                {
                    _updatedAddresses = updatedAddresses;
                }

                public IImmutableSet<Address> UpdatedAddresses => _updatedAddresses;
                public IImmutableDictionary<Address, IAccount> Accounts => throw new NotSupportedException();
            }
        }
    }
}
