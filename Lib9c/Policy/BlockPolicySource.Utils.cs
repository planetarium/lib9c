using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Bencodex.Types;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Model.State;
using static Libplanet.Blocks.BlockMarshaler;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain.Policy
{
    // Collection of helper methods not directly used as a pluggable component.
    public partial class BlockPolicySource
    {
        internal static bool IsObsolete(Transaction<NCAction> transaction, long blockIndex) =>
            transaction.CustomActions is { } customActions &&
            customActions
                .Select(action => action.InnerAction.GetType())
                .Any(
                    at =>
                    at.IsDefined(typeof(ActionObsoleteAttribute), false) &&
                    at.GetCustomAttributes()
                        .OfType<ActionObsoleteAttribute>()
                        .FirstOrDefault()?.ObsoleteIndex < blockIndex
                );

        internal static bool IsAdminTransaction(
            BlockChain<NCAction> blockChain, Transaction<NCAction> transaction)
        {
            return GetAdminState(blockChain) is AdminState admin
                && admin.AdminAddress.Equals(transaction.Signer);
        }

        internal static AdminState GetAdminState(
            BlockChain<NCAction> blockChain)
        {
            try
            {
                return blockChain.GetState(AdminState.Address) is Dictionary rawAdmin
                    ? new AdminState(rawAdmin)
                    : null;
            }
            catch (IncompleteBlockStatesException)
            {
                return null;
            }
        }

        private static bool IsAuthorizedMinerTransactionRaw(
            Transaction<NCAction> transaction,
            ImmutableHashSet<Address> allAuthorizedMiners)
        {
            return allAuthorizedMiners.Contains(transaction.Signer);
        }

        private static InvalidBlockBytesLengthException ValidateBlockBytesRaw(
            Block<NCAction> block,
            IVariableSubPolicy<long> maxBlockBytesPolicy)
        {
            long maxBlockBytes = maxBlockBytesPolicy.Getter(block.Index);
            long blockBytes = block.MarshalBlock().EncodingLength;

            if (blockBytes > maxBlockBytes)
            {
                return new InvalidBlockBytesLengthException(
                    $"The size of block #{block.Index} {block.Hash} is too large " +
                    $"where the maximum number of bytes allowed is {maxBlockBytes}: " +
                    $"{blockBytes}",
                    blockBytes);
            }

            return null;
        }

        private static BlockPolicyViolationException ValidateTxCountPerBlockRaw(
            Block<NCAction> block,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy)
        {
            int minTransactionsPerBlock =
                minTransactionsPerBlockPolicy.Getter(block.Index);
            int maxTransactionsPerBlock =
                maxTransactionsPerBlockPolicy.Getter(block.Index);

            if (block.Transactions.Count < minTransactionsPerBlock)
            {
                return new InvalidBlockTxCountException(
                    $"Block #{block.Index} {block.Hash} should include " +
                    $"at least {minTransactionsPerBlock} transaction(s): " +
                    $"{block.Transactions.Count}",
                    block.Transactions.Count);
            }
            else if (block.Transactions.Count > maxTransactionsPerBlock)
            {
                return new InvalidBlockTxCountException(
                    $"Block #{block.Index} {block.Hash} should include " +
                    $"at most {maxTransactionsPerBlock} transaction(s): " +
                    $"{block.Transactions.Count}",
                    block.Transactions.Count);
            }

            return null;
        }

        private static BlockPolicyViolationException ValidateTxCountPerSignerPerBlockRaw(
            Block<NCAction> block,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy)
        {
            int maxTransactionsPerSignerPerBlock =
                maxTransactionsPerSignerPerBlockPolicy.Getter(block.Index);
            var groups = block.Transactions
                .GroupBy(tx => tx.Signer)
                .Where(group => group.Count() > maxTransactionsPerSignerPerBlock);
            var offendingGroup = groups.FirstOrDefault();

            if (!(offendingGroup is null))
            {
                int offendingGroupCount = offendingGroup.Count();
                return new InvalidBlockTxCountPerSignerException(
                    $"Block #{block.Index} {block.Hash} includes too many " +
                    $"transactions from signer {offendingGroup.Key} where " +
                    $"the maximum number of transactions allowed by a single signer " +
                    $"per block is {maxTransactionsPerSignerPerBlock}: " +
                    $"{offendingGroupCount}",
                    offendingGroup.Key,
                    offendingGroupCount);
            }

            return null;
        }
    }
}
