using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Tx;
using System;
using System.Collections.Generic;
using Libplanet.Consensus;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain.Policy
{
    public class BlockPolicy : BlockPolicy<NCAction>
    {
        private readonly Func<Address, long, bool> _isAllowedToMine;

        public BlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            Func<BlockChain<NCAction>, Transaction<NCAction>, TxPolicyViolationException>
                validateNextBlockTx = null,
            Func<BlockChain<NCAction>, Block<NCAction>, BlockPolicyViolationException>
                validateNextBlock = null,
            Func<long, long> getMaxTransactionsBytes = null,
            Func<long, int> getMinTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerSignerPerBlock = null,
            Func<Address, long, bool> isAllowedToMine = null)
            : base(
                blockAction: blockAction,
                blockInterval: blockInterval,
                validateNextBlockTx: validateNextBlockTx,
                validateNextBlock: validateNextBlock,
                getMaxTransactionsBytes: getMaxTransactionsBytes,
                getMinTransactionsPerBlock: getMinTransactionsPerBlock,
                getMaxTransactionsPerBlock: getMaxTransactionsPerBlock,
                getMaxTransactionsPerSignerPerBlock: getMaxTransactionsPerSignerPerBlock)
        {
            _isAllowedToMine = isAllowedToMine;
        }

        public bool IsAllowedToMine(Address miner, long index) =>
            _isAllowedToMine(miner, index);
    }
}
