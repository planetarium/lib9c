using System.Collections.Immutable;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;

namespace Libplanet.Extensions.RemoteBlockChainStates;

public class RemoteAccount : IAccount
{
    private readonly IAccountState _baseState;

    public RemoteAccount(IAccountState baseState)
    {
        _baseState = baseState;
    }

    public IAccountDelta Delta => throw new NotImplementedException();

    public IImmutableSet<(Address, Currency)> TotalUpdatedFungibleAssets => throw new NotImplementedException();

    public Address Address => _baseState.Address;

    public HashDigest<SHA256>? StateRootHash => _baseState.StateRootHash;

    public BlockHash? BlockHash => _baseState.BlockHash;

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
        throw new NotImplementedException();
    }

    public IAccount BurnAsset(IActionContext context, Address owner, FungibleAssetValue value)
    {
        throw new NotImplementedException();
    }

    public IAccount SetState(Address address, IValue state)
    {
        throw new NotImplementedException();
    }

    public IAccount SetValidator(Validator validator)
    {
        throw new NotImplementedException();
    }

    public IAccount TransferAsset(IActionContext context, Address sender, Address recipient, FungibleAssetValue value, bool allowNegativeBalance = false)
    {
        throw new NotImplementedException();
    }
}
