using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action.Sys;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Crypto;
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

        private static BlockPolicyViolationException ValidateBlockProtocolVersionRaw(
            Block<NCAction> block,
            IVariableSubPolicy<int> minBlockProtocolVersionPolicy)
        {
            int minBlockProtocolVersion = minBlockProtocolVersionPolicy.Getter(block.Index);
            if (block.ProtocolVersion < minBlockProtocolVersion)
            {
                // NOTE: InvalidBlockProtocolVersionException would be more appropriate,
                // but it is not a BlockPolicyViolationException; as this is a temporary
                // solution to allow migration, we just use BlockPolicyViolationException.
                return new BlockPolicyViolationException(
                    $"The minimum block protocol version of block #{block.Index} is " +
                    $"{minBlockProtocolVersion} while the given block has " +
                    $"block protocol versoin {block.ProtocolVersion}.");
            }

            return null;
        }

        private static InvalidBlockBytesLengthException ValidateTransactionsBytesRaw(
            Block<NCAction> block,
            IVariableSubPolicy<long> maxTransactionsBytesPolicy)
        {
            long maxTransactionsBytes = maxTransactionsBytesPolicy.Getter(block.Index);
            long transactionsBytes = block.MarshalBlock().EncodingLength;

            if (transactionsBytes > maxTransactionsBytes)
            {
                return new InvalidBlockBytesLengthException(
                    $"The size of block #{block.Index} {block.Hash} is too large where " +
                    $"the maximum number of bytes allowed for transactions is " +
                    $"{maxTransactionsBytes}: {transactionsBytes}",
                    transactionsBytes);
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

        private static BlockPolicyViolationException ValidateMinerAuthorityRaw(
            Block<NCAction> block,
            IVariableSubPolicy<ImmutableHashSet<Address>> authorizedMinersPolicy)
        {
            // For genesis block, any miner can mine.
            if (block.Index == 0)
            {
                return null;
            }
            // If not an authorized mining block index, any miner can mine.
            else if (!authorizedMinersPolicy.IsTargetIndex(block.Index))
            {
                return null;
            }
            // Otherwise, block's miner should be one of the authorized miners.
            else if (authorizedMinersPolicy.Getter(block.Index).Contains(block.Miner))
            {
                return null;
            }
            else
            {
                return new BlockPolicyViolationException(
                    $"The block #{block.Index} {block.Hash} is not mined by an authorized miner.");
            }
        }

        private static BlockPolicyViolationException ValidateMinerPermissionRaw(
            Block<NCAction> block,
            IVariableSubPolicy<ImmutableHashSet<Address>> permissionedMinersPolicy)
        {
            // If the set of permissioned miners is empty, any miner can mine.
            if (!permissionedMinersPolicy.IsTargetIndex(block.Index))
            {
                return null;
            }
            else if (permissionedMinersPolicy.Getter(block.Index).Contains(block.Miner))
            {
                return null;
            }
            else
            {
                return new BlockPolicyViolationException(
                    $"Block #{block.Index} {block.Hash} is not mined by " +
                    $"a permissioned miner: {block.Miner}");
            }
        }

        private static bool IsAllowedToMineRaw(
            Address miner,
            long index,
            IVariableSubPolicy<ImmutableHashSet<Address>> authorizedMinersPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> permissionedMinersPolicy)
        {
            // For genesis blocks, any miner is allowed to mine.
            if (index == 0)
            {
                return true;
            }
            else if (authorizedMinersPolicy.IsTargetIndex(index))
            {
                return authorizedMinersPolicy.Getter(index).Contains(miner);
            }
            else if (permissionedMinersPolicy.IsTargetIndex(index))
            {
                return permissionedMinersPolicy.Getter(index).Contains(miner);
            }

            // If none of the conditions apply, any miner is allowed to mine.
            return true;
        }

        private static BlockPolicyViolationException ValidateSetValidatorActionRaw(
            Block<NCAction> block,
            IVariableSubPolicy<PublicKey> validatorAdminPolicy)
        {
            PublicKey validatorAdmin = validatorAdminPolicy.Getter(block.Index);
            var logDict = new Dictionary<Transaction<NCAction>, PublicKey>();
            foreach (Transaction<NCAction> transaction in block.Transactions)
            {
                if (transaction.SystemAction is SetValidator &&
                    !transaction.PublicKey.Equals(validatorAdmin))
                {
                    logDict.Add(transaction, transaction.PublicKey);
                }
            }

            var sortedLogDict = logDict.OrderBy(item => item.Value.ToAddress());
            if (sortedLogDict.Any())
            {
                string logStr = sortedLogDict.Aggregate("", (total, next)
                    => total + $"On transaction {next.Key.Id}, " +
                    $"expected to be signed by validor admin {validatorAdmin}, " +
                    $"but signed by {next.Value}\n");

                return new BlockPolicyViolationException(
                    $"Block #{block.Index} {block.Hash} includes " +
                    $"transactions that contains invalid SetValidator action : \n" +
                    $"{logStr}");
            }

            return null;
        }
    }
}
