namespace Lib9c.Tests.Action
{
#nullable enable
    using System;
    using System.Collections.Immutable;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Types.Blocks;

    public class MockWorldState : IWorldState
    {
        private static readonly MockWorldState _empty = new MockWorldState();
        private readonly IImmutableDictionary<Address, IAccount> _accounts;

        private MockWorldState()
            : this(ImmutableDictionary<Address, IAccount>.Empty)
        {
        }

        private MockWorldState(IImmutableDictionary<Address, IAccount> accounts)
        {
            _accounts = accounts;
            Trie = new TrieStateStore(new MemoryKeyValueStore()).GetStateRoot(null);
        }

        public static MockWorldState Empty => _empty;

        /// <inheritdoc/>
        public ITrie Trie { get; }

        /// <inheritdoc/>
        public bool Legacy => false;

        /// <inheritdoc/>
        public BlockHash? BlockHash => null;

        public IImmutableDictionary<Address, IAccount> Accounts => _accounts;

        /// <inheritdoc/>
        public IAccount GetAccount(Address address) => _accounts.TryGetValue(address, out IAccount? account)
            ? account
            : new MockAccount(address);
    }
}
