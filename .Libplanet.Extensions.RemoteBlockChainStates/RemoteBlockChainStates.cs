using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
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

        public IAccountState GetAccount(Address address, HashDigest<SHA256>? offset)
        {
            return new RemoteWorldState(_explorerEndpoint, offset).GetAccount(address);
        }

        public ITrie GetBlockStateRoot(BlockHash? offset)
        {
            throw new NotImplementedException();
        }

        public ITrie GetStateRoot(HashDigest<SHA256>? offset)
        {
            throw new NotImplementedException();
        }

        public IValue? GetState(Address address, Address accountAddress, BlockHash? offset) =>
            GetStates(new[] { address }, accountAddress, offset).First();

        public IReadOnlyList<IValue?> GetStates(IReadOnlyList<Address> addresses, Address accountAddress, BlockHash? offset)
        {
            return new RemoteWorldState(_explorerEndpoint, offset).GetStates(addresses);
        }

        public FungibleAssetValue GetBalance(Address address, Currency currency, BlockHash? offset)
        {
            return new RemoteWorldState(_explorerEndpoint, offset).GetBalance(address, currency);
        }

        public FungibleAssetValue GetTotalSupply(Currency currency, BlockHash? offset)
        {
            return new RemoteWorldState(_explorerEndpoint, offset).GetTotalSupply(currency);
        }

        public ValidatorSet GetValidatorSet(BlockHash? offset)
        {
            return new RemoteWorldState(_explorerEndpoint, offset).GetValidatorSet();
        }

        public IWorldState GetWorldState(BlockHash? offset)
        {
            return new RemoteWorldState(_explorerEndpoint, offset);
        }
    }
}
