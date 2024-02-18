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
    }
}
