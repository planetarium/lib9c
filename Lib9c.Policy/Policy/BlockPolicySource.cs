using System;
using System.Collections.Immutable;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Action.State;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Nekoyume.Action;
using Nekoyume.Action.Loader;
using Nekoyume.Action.ValidatorDelegation;

#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
#endif

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions;
using Lib9c.DevExtensions.Model;
#endif

namespace Nekoyume.Blockchain.Policy
{
    public partial class BlockPolicySource
    {
        public static int MaxTransactionsPerBlock;

        public const int DefaultMaxTransactionsPerBlock = 200;

        public static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(8);

        private readonly IActionLoader _actionLoader;

        public BlockPolicySource(
            IActionLoader? actionLoader = null,
            int? maxTransactionPerBlock = null)
        {
            _actionLoader = actionLoader ?? new NCActionLoader();
            MaxTransactionsPerBlock = Math.Min(
                maxTransactionPerBlock ?? DefaultMaxTransactionsPerBlock,
                DefaultMaxTransactionsPerBlock);
        }

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for Odin mainnet.
        /// </summary>
        public IBlockPolicy GetPolicy() => GetPolicy(Planet.Odin);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for the given planet.
        /// </summary>
        public IBlockPolicy GetPolicy(Planet planet)
        {
            return planet switch
            {
                Planet.Odin => GetPolicy(
                    maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Odin,
                    minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Odin,
                    maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Odin,
                    maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Odin
                ),
                Planet.OdinInternal => GetPolicy(
                    maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.OdinInternal,
                    minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Odin,
                    maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Odin,
                    maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.OdinInternal
                ),
                Planet.Heimdall => GetPolicy(
                    maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Heimdall,
                    minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Heimdall,
                    maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Heimdall,
                    maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Heimdall
                ),
                Planet.HeimdallInternal => GetPolicy(
                    maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Heimdall,
                    minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Heimdall,
                    maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Heimdall,
                    maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.HeimdallInternal
                ),
                Planet.Thor => GetPolicy(
                    maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Thor,
                    minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Thor,
                    maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Thor,
                    maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Thor
                ),
                Planet.ThorInternal => GetPolicy(
                    maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Thor,
                    minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Thor,
                    maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Thor,
                    maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.ThorInternal
                ),
                _ => throw new ArgumentException(
                    $"Can't retrieve policy for given planet ({planet})",
                    nameof(planet)
                ),
            };
        }

        /// <summary>
        /// Gets a <see cref="BlockPolicy"/> constructed from given parameters.
        /// </summary>
        /// <param name="minimumDifficulty">The minimum difficulty that a <see cref="Block{T}"/>
        /// can have.  This is ignored for genesis blocks.</param>
        /// <param name="minTransactionsPerBlockPolicy">Used for minimum number of transactions
        /// required per block.</param>
        /// <param name="maxTransactionsPerBlockPolicy">The maximum number of
        /// <see cref="Transaction{T}"/>s that a <see cref="Block{T}"/> can have.</param>
        /// <param name="maxTransactionsPerSignerPerBlockPolicy">The maximum number of
        /// <see cref="Transaction{T}"/>s from a single miner that a <see cref="Block{T}"/>
        /// can have.</param>
        /// <returns>A <see cref="BlockPolicy"/> constructed from given parameters.</returns>
        internal IBlockPolicy GetPolicy(
            IVariableSubPolicy<long> maxTransactionsBytesPolicy,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy)
        {
            maxTransactionsBytesPolicy = maxTransactionsBytesPolicy
                ?? MaxTransactionsBytesPolicy.Default;
            minTransactionsPerBlockPolicy = minTransactionsPerBlockPolicy
                ?? MinTransactionsPerBlockPolicy.Default;
            maxTransactionsPerBlockPolicy = maxTransactionsPerBlockPolicy
                ?? MaxTransactionsPerBlockPolicy.Default;
            maxTransactionsPerSignerPerBlockPolicy = maxTransactionsPerSignerPerBlockPolicy
                ?? MaxTransactionsPerSignerPerBlockPolicy.Default;

            Func<BlockChain, Transaction, TxPolicyViolationException?> validateNextBlockTx =
                (blockChain, transaction) => ValidateNextBlockTxRaw(
                    blockChain, _actionLoader, transaction);
            Func<BlockChain, Block, BlockPolicyViolationException?> validateNextBlock =
                (blockchain, block) => ValidateNextBlockRaw(
                    block,
                    maxTransactionsBytesPolicy,
                    minTransactionsPerBlockPolicy,
                    maxTransactionsPerBlockPolicy,
                    maxTransactionsPerSignerPerBlockPolicy);

            // FIXME: Slight inconsistency due to pre-existing delegate.
            return new BlockPolicy(
                policyActionsRegistry: new PolicyActionsRegistry(
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
                    }.ToImmutableArray()),
                blockInterval: BlockInterval,
                validateNextBlockTx: validateNextBlockTx,
                validateNextBlock: validateNextBlock,
                getMaxTransactionsBytes: maxTransactionsBytesPolicy.Getter,
                getMinTransactionsPerBlock: minTransactionsPerBlockPolicy.Getter,
                getMaxTransactionsPerBlock: maxTransactionsPerBlockPolicy.Getter,
                getMaxTransactionsPerSignerPerBlock: maxTransactionsPerSignerPerBlockPolicy.Getter);
        }

        // TODO: Remove BlockChain.GetNextWorldState() from below method.
        internal static TxPolicyViolationException? ValidateNextBlockTxRaw(
            BlockChain blockChain,
            IActionLoader actionLoader,
            Transaction transaction)
        {
            // Avoid NRE when genesis block appended
            long index = blockChain.Count > 0 ? blockChain.Tip.Index + 1 : 0;

            if (transaction.Actions?.Count > 1)
            {
                return new TxPolicyViolationException(
                    $"Transaction {transaction.Id} has too many actions: " +
                    $"{transaction.Actions?.Count}",
                    transaction.Id);
            }
            else if (IsObsolete(transaction, actionLoader, index))
            {
                return new TxPolicyViolationException(
                    $"Transaction {transaction.Id} is obsolete.",
                    transaction.Id);
            }

            try
            {
                // This block is used to bypass transactions before mead has been created.
                // It's no longer required for future transactions, so better to be removed later.
                if (blockChain
                    .GetWorldState()
                    .GetBalance(MeadConfig.PatronAddress, Currencies.Mead) < 1 * Currencies.Mead)
                {
                    return null;
                }

                if (!(transaction.MaxGasPrice is { } gasPrice && transaction.GasLimit is { } gasLimit))
                {
                    return new TxPolicyViolationException(
                        "Transaction has no gas price or limit.",
                        transaction.Id);
                }

                if (gasPrice.Sign < 0 || gasLimit < 0)
                {
                    return new TxPolicyViolationException(
                        "Transaction has negative gas price or limit.",
                        transaction.Id);
                }

            }
            catch (InvalidSignatureException)
            {
                return new TxPolicyViolationException(
                    $"Transaction {transaction.Id} has invalid signature.",
                    transaction.Id);
            }

            return null;
        }

        internal static BlockPolicyViolationException? ValidateNextBlockRaw(
            Block nextBlock,
            IVariableSubPolicy<long> maxTransactionsBytesPolicy,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy)
        {
            if (ValidateTransactionsBytesRaw(
                nextBlock,
                maxTransactionsBytesPolicy) is InvalidBlockBytesLengthException ibble)
            {
                return ibble;
            }

            if (ValidateTxCountPerBlockRaw(
                nextBlock,
                minTransactionsPerBlockPolicy,
                maxTransactionsPerBlockPolicy) is InvalidBlockTxCountException ibtce)
            {
                return ibtce;
            }

            if (ValidateTxCountPerSignerPerBlockRaw(
                nextBlock,
                maxTransactionsPerSignerPerBlockPolicy) is InvalidBlockTxCountPerSignerException ibtcpse)
            {
                return ibtcpse;
            }

            return null;
        }

        private class ActionTypeLoaderContext : IActionTypeLoaderContext
        {
            public ActionTypeLoaderContext(long index)
            {
                Index = index;
            }

            public long Index { get; }
        }
    }
}
