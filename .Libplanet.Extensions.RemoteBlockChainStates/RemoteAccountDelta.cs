using System.Collections.Immutable;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Consensus;

namespace Libplanet.Extensions.RemoteBlockChainStates;

public class RemoteAccountDelta : IAccountDelta
{
    public RemoteAccountDelta()
    {
    }

    public IImmutableSet<Address> UpdatedAddresses => throw new NotImplementedException();

    public IImmutableSet<Address> StateUpdatedAddresses => throw new NotImplementedException();

    public IImmutableDictionary<Address, IValue> States => throw new NotImplementedException();

    public IImmutableSet<Address> FungibleUpdatedAddresses => throw new NotImplementedException();

    public IImmutableSet<(Address, Currency)> UpdatedFungibleAssets => throw new NotImplementedException();

    public IImmutableDictionary<(Address, Currency), BigInteger> Fungibles => throw new NotImplementedException();

    public IImmutableSet<Currency> UpdatedTotalSupplyCurrencies => throw new NotImplementedException();

    public IImmutableDictionary<Currency, BigInteger> TotalSupplies => throw new NotImplementedException();

    public ValidatorSet? ValidatorSet => throw new NotImplementedException();
}
