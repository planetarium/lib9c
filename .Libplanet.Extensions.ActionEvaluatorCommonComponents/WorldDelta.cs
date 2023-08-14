using System.Collections.Immutable;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents;

public class WorldDelta : IWorldDelta
{
    public WorldDelta()
    {
        Accounts = ImmutableDictionary<Address, IAccount>.Empty;
    }
    public WorldDelta(IImmutableDictionary<Address, IAccount> accountDelta)
    {
        Accounts = accountDelta;
    }

    public IImmutableDictionary<Address, IAccount> Accounts { get; }

    public IImmutableSet<Address> UpdatedAddresses => Accounts.Keys.ToImmutableHashSet();
}
