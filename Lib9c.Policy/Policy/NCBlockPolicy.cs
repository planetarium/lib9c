using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using System;
using System.Collections.Immutable;

namespace Nekoyume.Blockchain.Policy
{
    public class NCBlockPolicy : BlockPolicy
    {
        public NCBlockPolicy(
            ImmutableArray<IAction> beginBlockActions,
            ImmutableArray<IAction> endBlockActions,
            TimeSpan blockInterval,
            Func<BlockChain, Transaction, TxPolicyViolationException>?
                validateNextBlockTx = null,
            Func<BlockChain, Block, BlockPolicyViolationException>?
                validateNextBlock = null,
            Func<long, long>? getMaxTransactionsBytes = null,
            Func<long, int>? getMinTransactionsPerBlock = null,
            Func<long, int>? getMaxTransactionsPerBlock = null,
            Func<long, int>? getMaxTransactionsPerSignerPerBlock = null)
            : base(
                beginBlockActions: beginBlockActions,
                endBlockActions: endBlockActions,
                blockInterval: blockInterval,
                validateNextBlockTx: validateNextBlockTx,
                validateNextBlock: validateNextBlock,
                getMaxTransactionsBytes: getMaxTransactionsBytes,
                getMinTransactionsPerBlock: getMinTransactionsPerBlock,
                getMaxTransactionsPerBlock: getMaxTransactionsPerBlock,
                getMaxTransactionsPerSignerPerBlock: getMaxTransactionsPerSignerPerBlock)
        {
        }
    }
}
