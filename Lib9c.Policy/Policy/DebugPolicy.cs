using System.Collections.Immutable;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Nekoyume.Action;
using Nekoyume.Action.DPoS;
using Nekoyume.Action.DPoS.Sys;

namespace Nekoyume.Blockchain.Policy
{
    public class DebugPolicy : IBlockPolicy
    {
        public DebugPolicy()
        {
        }

        public ImmutableArray<IAction> BeginBlockActions { get; } =
            new IAction[] { new AllocateReward() }.ToImmutableArray();

        public ImmutableArray<IAction> EndBlockActions { get; } =
            new IAction[] { new UpdateValidators(), new RecordProposer() }.ToImmutableArray();

        public ImmutableArray<IAction> BeginTxActions { get; } =
            new IAction[] { new Mortgage() }.ToImmutableArray();

        public ImmutableArray<IAction> EndTxActions { get; } =
            new IAction[] { new Refund(), new Reward() }.ToImmutableArray();

        public TxPolicyViolationException ValidateNextBlockTx(
            BlockChain blockChain, Transaction transaction)
        {
            return null;
        }

        public BlockPolicyViolationException ValidateNextBlock(
            BlockChain blockChain, Block nextBlock)
        {
            return null;
        }

        public long GetMaxTransactionsBytes(long index) => long.MaxValue;

        public int GetMinTransactionsPerBlock(long index) => 0;

        public int GetMaxTransactionsPerBlock(long index) => int.MaxValue;

        public int GetMaxTransactionsPerSignerPerBlock(long index) => int.MaxValue;

        public int GetMinBlockProtocolVersion(long index) => 0;
    }
}
