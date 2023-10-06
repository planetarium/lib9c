namespace Lib9c.Tests.Action
{
#nullable enable
    using System.Collections.Immutable;
    using System.Diagnostics.Contracts;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Store.Trie;

    /// <summary>
    /// A rough replica of https://github.com/planetarium/libplanet/blob/main/Libplanet/State/World.cs
    /// except this has its constructors exposed as public for testing.
    /// </summary>
    [Pure]
    public class MockWorld : IWorld
    {
        private readonly IWorldState _baseState;

        public MockWorld()
            : this(new MockWorldState())
        {
        }

        public MockWorld(Address address, IAccount account)
            : this(
                new MockWorldState(),
                new MockWorldDelta(
                    ImmutableDictionary<Address, IAccount>.Empty.SetItem(address, account)))
        {
        }

        public MockWorld(IWorldState baseState)
            : this(baseState, new MockWorldDelta())
        {
        }

        private MockWorld(IWorldState baseState, IWorldDelta delta)
        {
            _baseState = baseState;
            Delta = delta;
        }

        /// <inheritdoc/>
        public ITrie Trie => _baseState.Trie;

        /// <inheritdoc/>
        public bool Legacy => _baseState.Legacy;

        /// <inheritdoc/>
        public IWorldDelta Delta { get; private set; }

        /// <inheritdoc/>
        public IAccount GetAccount(Address address)
        {
            return Delta.Accounts.TryGetValue(address, out IAccount? account)
                ? account!
                : _baseState.GetAccount(address);
        }

        /// <inheritdoc/>
        public IWorld SetAccount(Address address, IAccount account)
        {
            if (!address.Equals(ReservedAddresses.LegacyAccount)
                && account.Delta.UpdatedFungibleAssets.Count > 0)
            {
                return this;
            }

            return new MockWorld(
                this,
                new MockWorldDelta(Delta.Accounts.SetItem(address, account)));
        }
    }
}
