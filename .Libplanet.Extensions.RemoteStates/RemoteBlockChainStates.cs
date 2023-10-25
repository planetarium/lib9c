using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;

namespace Libplanet.Extensions.RemoteStates
{
    public class RemoteBlockChainStates : IBlockChainStates
    {
        private readonly Uri _explorerEndpoint;

        public RemoteBlockChainStates(Uri explorerEndpoint)
        {
            _explorerEndpoint = explorerEndpoint;
        }

        public IWorldState GetWorldState(HashDigest<SHA256>? offsetStateRootHash)
            => new RemoteWorldState(_explorerEndpoint, offsetStateRootHash);

        public IWorldState GetWorldState(BlockHash? offsetBlockHash)
            => new RemoteWorldState(_explorerEndpoint, offsetBlockHash);

        public IAccountState GetAccountState(HashDigest<SHA256>? accountStateRootHash)
            => new RemoteAccountState(_explorerEndpoint, accountStateRootHash);

        public IAccountState GetAccountState(Address address, BlockHash? offsetBlockHash)
            => new RemoteAccountState(_explorerEndpoint, address, offsetBlockHash);

        public IValue? GetState(Address address, Address accountAddress, BlockHash? offsetBlockHash)
            => new RemoteAccountState(_explorerEndpoint, accountAddress, offsetBlockHash)
                .GetState(address);

        public IValue? GetState(Address address, HashDigest<SHA256>? accountStateRootHash)
            => new RemoteAccountState(_explorerEndpoint, accountStateRootHash)
                .GetState(address);

        public FungibleAssetValue GetBalance(
            Address address,
            Currency currency,
            HashDigest<SHA256>? hashDigest)
            => new RemoteAccountState(_explorerEndpoint, hashDigest)
                .GetBalance(address, currency);

        public FungibleAssetValue GetBalance(
            Address address,
            Currency currency,
            Address accountAddress,
            BlockHash? offset)
            => new RemoteAccountState(_explorerEndpoint, accountAddress, offset)
                .GetBalance(address, currency);

        public FungibleAssetValue GetTotalSupply(Currency currency, HashDigest<SHA256>? hashDigest)
            => new RemoteAccountState(_explorerEndpoint, hashDigest)
                .GetTotalSupply(currency);

        public FungibleAssetValue GetTotalSupply(Currency currency, Address accountAddress, BlockHash? offset)
            => new RemoteAccountState(_explorerEndpoint, accountAddress, offset)
                .GetTotalSupply(currency);

        public ValidatorSet GetValidatorSet(HashDigest<SHA256>? hashDigest)
            => new RemoteAccountState(_explorerEndpoint, hashDigest)
                .GetValidatorSet();

        public ValidatorSet GetValidatorSet(Address accountAddress, BlockHash? offset)
            => new RemoteAccountState(_explorerEndpoint, accountAddress, offset)
                .GetValidatorSet();
    }
}
