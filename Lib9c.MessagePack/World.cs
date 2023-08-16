using System.Collections.Immutable;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;

namespace Lib9c.Formatters
{
    public struct World : IWorld
    {
        private IImmutableDictionary<Address, IAccount> _accounts;
        private MockWorldDelta _delta;

        public World(IImmutableDictionary<Address, IAccount> accounts)
        {
            _delta = new MockWorldDelta(accounts);
            _accounts = accounts;
        }

        public World(Dictionary accounts)
            : this(
                accounts.ToImmutableDictionary(
                    kv => new Address(kv.Key),
                    kv => (IAccount)new Account(kv.Value)))
        {
        }

        public World(IValue serialized)
            : this(
                (Dictionary)((Dictionary)serialized)["accounts"]
            )
        {
        }

        public World(byte[] bytes)
            : this((Dictionary)new Codec().Decode(bytes))
        {
        }

        public IWorldDelta Delta => _delta;

        public BlockHash? BlockHash => null;

        public bool Legacy => true;

        public IAccount GetAccount(Address address) =>
            _accounts.ContainsKey(address)
                ? _accounts[address]
                : new Account();

        public IWorld SetAccount(IAccount account) =>
            new World(_accounts.SetItem(account.Address, account));

        private class MockWorldDelta : IWorldDelta
        {
            private IImmutableDictionary<Address, IAccount> _accounts;

            public MockWorldDelta(
                IImmutableDictionary<Address, IAccount> accounts)
            {
                _accounts = accounts;
            }

            public IImmutableSet<Address> UpdatedAddresses => _accounts.Keys.ToImmutableHashSet();
            public IImmutableDictionary<Address, IAccount> Accounts => _accounts;
        }
    }
}
