using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Consensus;

namespace Libplanet.Extensions.RemoteBlockChainStates;

public class RemoteAccount : IAccount
{
    private readonly IAccountState _baseState;

    public RemoteAccount(IAccountState baseState)
    {
        _baseState = baseState;
    }

    public IImmutableSet<(Address, Currency)> TotalUpdatedFungibleAssets => throw new NotSupportedException();

    public ITrie Trie => _baseState.Trie;

    public FungibleAssetValue GetBalance(Address address, Currency currency)
        => _baseState.GetBalance(address, currency);

    public IValue? GetState(Address address)
        => _baseState.GetState(address);

    public IReadOnlyList<IValue?> GetStates(IReadOnlyList<Address> addresses)
        => _baseState.GetStates(addresses);

    public FungibleAssetValue GetTotalSupply(Currency currency)
        => _baseState.GetTotalSupply(currency);

    public ValidatorSet GetValidatorSet()
        => _baseState.GetValidatorSet();

    public IAccount MintAsset(IActionContext context, Address recipient, FungibleAssetValue value)
    {
        throw new NotSupportedException();
    }

    public IAccount BurnAsset(IActionContext context, Address owner, FungibleAssetValue value)
    {
        throw new NotSupportedException();
    }

    public IAccount SetState(Address address, IValue state)
    {
        throw new NotSupportedException();
    }

    public IAccount RemoveState(Address address)
    {
        throw new NotSupportedException();
    }

    public IAccount SetValidator(Validator validator)
    {
        throw new NotSupportedException();
    }

    public IAccount TransferAsset(IActionContext context, Address sender, Address recipient, FungibleAssetValue value, bool allowNegativeBalance = false)
    {
        throw new NotSupportedException();
    }
}
