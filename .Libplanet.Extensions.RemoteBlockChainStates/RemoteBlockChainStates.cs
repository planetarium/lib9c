using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
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

        public IWorldState GetWorldState(HashDigest<SHA256>? offsetStateRootHash)
            => new RemoteWorldState(_explorerEndpoint, offsetStateRootHash);

        public IWorldState GetWorldState(BlockHash? offsetBlockHash)
            => new RemoteWorldState(_explorerEndpoint, offsetBlockHash);

        public IAccountState GetAccountState(HashDigest<SHA256>? accountStateRootHash)
            => new RemoteAccountState(_explorerEndpoint, accountStateRootHash);

        public IAccountState GetAccountState(BlockHash? offsetBlockHash, Address address)
            => new RemoteAccountState(_explorerEndpoint, offsetBlockHash, address);

        public IValue? GetState(BlockHash? offsetBlockHash, Address accountAddress, Address address)
            => new RemoteAccountState(_explorerEndpoint, offsetBlockHash, accountAddress)
                .GetState(address);

        public IValue? GetState(HashDigest<SHA256>? accountStateRootHash, Address address)
            => new RemoteAccountState(_explorerEndpoint, accountStateRootHash)
                .GetState(address);

        public FungibleAssetValue GetBalance(
            HashDigest<SHA256>? hashDigest,
            Address address,
            Currency currency)
            => new RemoteAccountState(_explorerEndpoint, hashDigest)
                .GetBalance(address, currency);

        public FungibleAssetValue GetBalance(
            BlockHash? offset,
            Address address,
            Currency currency)
            => new RemoteAccountState(_explorerEndpoint, offset, ReservedAddresses.LegacyAccount)
                .GetBalance(address, currency);

        public FungibleAssetValue GetTotalSupply(HashDigest<SHA256>? hashDigest, Currency currency)
            => new RemoteAccountState(_explorerEndpoint, hashDigest)
                .GetTotalSupply(currency);

        public FungibleAssetValue GetTotalSupply(BlockHash? offset, Currency currency)
            => new RemoteAccountState(_explorerEndpoint, offset, ReservedAddresses.LegacyAccount)
                .GetTotalSupply(currency);

        public ValidatorSet GetValidatorSet(HashDigest<SHA256>? hashDigest)
            => new RemoteAccountState(_explorerEndpoint, hashDigest)
                .GetValidatorSet();

        public ValidatorSet GetValidatorSet(BlockHash? offset)
            => new RemoteAccountState(_explorerEndpoint, offset, ReservedAddresses.LegacyAccount)
                .GetValidatorSet();
    }
}
