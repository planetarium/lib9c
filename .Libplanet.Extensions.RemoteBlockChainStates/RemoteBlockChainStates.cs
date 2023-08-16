using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;

namespace Libplanet.Extensions.RemoteBlockChainStates
{
    public class RemoteBlockChainStates : IBlockChainStates
    {
        private readonly Uri _explorerEndpoint;

        public RemoteBlockChainStates(Uri explorerEndpoint)
        {
            _explorerEndpoint = explorerEndpoint;
        }

        public IWorldState GetWorldState(BlockHash? blockHash)
            => new RemoteWorldState(
                _explorerEndpoint,
                blockHash);

        public IAccountState GetBlockAccountState(Address address, BlockHash? offset)
            => new RemoteWorldState(
                _explorerEndpoint,
                offset).GetAccount(address);

        public IAccountState GetAccountState(Address address, BlockHash? offset)
            => new RemoteAccountState(
                _explorerEndpoint,
                address,
                offset);

        public ITrie GetBlockStateRoot(BlockHash? offset)
        {
            throw new NotImplementedException();
        }

        public ITrie GetStateRoot(BlockHash? offset)
        {
            throw new NotImplementedException();
        }

        public IValue? GetState(Address address, Address accountAddress, BlockHash? offset) =>
            GetStates(new[] { address }, accountAddress, offset).First();

        public IReadOnlyList<IValue?> GetStates(IReadOnlyList<Address> addresses, Address accountAddress, BlockHash? offset)
            => new RemoteWorldState(_explorerEndpoint, offset).GetAccount(
                accountAddress).GetStates(addresses);

        public FungibleAssetValue GetBalance(Address address, Currency currency, BlockHash? offset)
            => new RemoteWorldState(_explorerEndpoint, offset).GetAccount(
                ReservedAddresses.LegacyAccount).GetBalance(address, currency);

        public FungibleAssetValue GetTotalSupply(Currency currency, BlockHash? offset)
            => new RemoteWorldState(_explorerEndpoint, offset).GetAccount(
                ReservedAddresses.LegacyAccount).GetTotalSupply(currency);

        public ValidatorSet GetValidatorSet(BlockHash? offset)
            => new RemoteWorldState(_explorerEndpoint, offset).GetAccount(
                ReservedAddresses.LegacyAccount).GetValidatorSet();
    }
}
