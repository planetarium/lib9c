using System.Collections.Immutable;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Nekoyume.Action;
using Nekoyume.Action.ValidatorDelegation;

namespace Nekoyume.Blockchain.Policy
{
    public class DebugPolicy : IBlockPolicy
    {
        public DebugPolicy()
        {
        }

        public IPolicyActionsRegistry PolicyActionsRegistry { get; } =
            new PolicyActionsRegistry(
                beginBlockActions: new IAction[] {
                    new SlashValidator(),
                    new AllocateGuildReward(),
                    new AllocateReward(),
                }.ToImmutableArray(),
                endBlockActions: new IAction[] {
                    new UpdateValidators(),
                    new RecordProposer(),
                    new RewardGold(),
                    new ReleaseValidatorUnbondings(),
                }.ToImmutableArray(),
                beginTxActions: new IAction[] {
                    new Mortgage(),
                }.ToImmutableArray(),
                endTxActions: new IAction[] {
                    new Reward(), new Refund(),
                }.ToImmutableArray());

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

        public long GetMaxEvidencePendingDuration(long index) => 10L;
    }
}
