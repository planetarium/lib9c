namespace Lib9c.Tests.Action
{
#nullable enable
    using System.Collections.Immutable;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;

    public class MockWorldState : IWorldState
    {
        private readonly IImmutableDictionary<Address, IAccount> _accounts;

        public MockWorldState()
            : this(ImmutableDictionary<Address, IAccount>.Empty)
        {
        }

        public MockWorldState(IImmutableDictionary<Address, IAccount> accounts)
        {
            _accounts = accounts;
            Trie = new TrieStateStore(new MemoryKeyValueStore()).GetStateRoot(null);
        }

        /// <inheritdoc/>
        public ITrie Trie { get; }

        /// <inheritdoc/>
        public bool Legacy => false;

        public IImmutableDictionary<Address, IAccount> Accounts => _accounts;

        /// <inheritdoc/>
        public IAccount GetAccount(Address address) => _accounts.TryGetValue(address, out IAccount? account)
            ? account
            : new MockAccount();
    }
}
