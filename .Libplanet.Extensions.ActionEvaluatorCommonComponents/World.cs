using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Store.Trie;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents;

public class World : IWorld
{
    private IImmutableDictionary<Address, IAccount> _accounts;
    private IWorldDelta _delta;

    public World()
        : this(ImmutableDictionary<Address, IAccount>.Empty)
    {
    }

    public World(IImmutableDictionary<Address, IAccount> accounts)
    {
        _delta = new WorldDelta(accounts);
        _accounts = accounts;
    }

    public World(Dictionary accounts)
    {
        // This assumes `states` consists of only Binary keys:
        _accounts = accounts
            .ToImmutableDictionary(
                kv => new Address(((Binary)kv.Key).ByteArray),
                kv => (IAccount)new Account(kv.Value));

        _delta = new WorldDelta(_accounts);
    }

    public World(IValue serialized)
        : this((Dictionary)serialized)
    {
    }

    public ITrie Trie => throw new NotSupportedException();

    public bool Legacy { get; }

    public IWorldDelta Delta => _delta;

    public IWorldState BaseState { get; set; }

    public IAccount GetAccount(Address address)
        => _accounts.ContainsKey(address)
            ? _accounts[address]
            : BaseState.GetAccount(address);

    public IWorld SetAccount(Address address, IAccount account)
        => new World(_accounts.SetItem(address, account));
}
