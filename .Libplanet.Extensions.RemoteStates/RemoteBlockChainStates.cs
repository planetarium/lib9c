using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using System.Security.Cryptography;

namespace Libplanet.Extensions.RemoteStates
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

        public IAccountState GetAccountState(Address address, BlockHash? offset)
            => new RemoteWorldState(
                _explorerEndpoint,
                offset).GetAccount(address);
        public IValue? GetState(Address address, Address accountAddress, BlockHash? offset)
            => new RemoteWorldState(_explorerEndpoint, offset).GetAccount(accountAddress).GetState(address);

        public FungibleAssetValue GetBalance(Address address, Currency currency, BlockHash? offset)
            => new RemoteWorldState(_explorerEndpoint, offset).GetAccount(
                ReservedAddresses.LegacyAccount).GetBalance(address, currency);

        public FungibleAssetValue GetTotalSupply(Currency currency, BlockHash? offset)
            => new RemoteWorldState(_explorerEndpoint, offset).GetAccount(
                ReservedAddresses.LegacyAccount).GetTotalSupply(currency);

        public ValidatorSet GetValidatorSet(BlockHash? offset)
            => new RemoteWorldState(_explorerEndpoint, offset).GetAccount(
                ReservedAddresses.LegacyAccount).GetValidatorSet();

        public IWorldState GetWorldState(HashDigest<SHA256>? hash)
        {
            throw new NotImplementedException();
        }

        public IAccountState GetAccountState(HashDigest<SHA256>? hash)
        {
            throw new NotImplementedException();
        }

        public IValue? GetState(Address address, HashDigest<SHA256>? hash)
        {
            throw new NotImplementedException();
        }
    }
}
