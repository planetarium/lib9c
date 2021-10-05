using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Renderer;
using Libplanet.Blocks;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Tx;
using Libplanet;
using Libplanet.Blockchain.Renderers;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Serilog;
using Serilog.Events;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
#endif
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain.Policy
{
    public partial class BlockPolicySource
    {
        public const int DifficultyStability = 2048;

        // Note: The heaviest block of 9c-main (except for the genesis) weighs 58,408 B (58 KiB).
        public const int MaxBlockBytes = 1024 * 100; // 100 KiB

        // Note: The genesis block of 9c-main net weighs 11,085,640 B (11 MiB).
        public const int MaxGenesisBytes = 1024 * 1024 * 15; // 15 MiB

        /// <summary>
        /// Last index in which restriction will apply.
        /// </summary>
        public const long AuthorizedMiningPolicyEndIndex = 3_153_600;

        public const long AuthorizedMiningPolicyInterval = 50;

        /// <summary>
        /// First index in which restriction will apply.
        /// </summary>
        public const long AuthorizedMiningNoOpTxHardcodedIndex = 1_200_001;

        /// <summary>
        /// First index in which restriction will apply.
        /// </summary>
        public const long MinTransactionsPerBlockHardcodedIndex = 2_173_701;

        public const int MinTransactionsPerBlock = 1;

        // FIXME: Should be finalized before release.
        public const long MaxTransactionsPerSignerPerBlockHardcodedIndex = 3_000_001;

        // FIXME: Should be finalized before release.
        public const int MaxTransactionsPerSignerPerBlock = 4;

        public const long V100080ObsoleteIndex = 2_448_000;

        // FIXME: Should be finalized before release.
        public const long V100081ObsoleteIndex = 2_500_000;

        public const long PermissionedMiningHardcodedIndex = 2_225_500;

        public readonly Dictionary<long, HashAlgorithmType> HashAlgorithmTable =
            new Dictionary<long, HashAlgorithmType> { [0] = HashAlgorithmType.Of<SHA256>() };

        public readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(8);

        public readonly ActionRenderer ActionRenderer = new ActionRenderer();

        public readonly BlockRenderer BlockRenderer = new BlockRenderer();

        public readonly LoggedActionRenderer<NCAction> LoggedActionRenderer;

        public readonly LoggedRenderer<NCAction> LoggedBlockRenderer;

        public BlockPolicySource(
            ILogger logger,
            LogEventLevel logEventLevel = LogEventLevel.Verbose)
        {
            LoggedActionRenderer =
                new LoggedActionRenderer<NCAction>(ActionRenderer, logger, logEventLevel);

            LoggedBlockRenderer =
                new LoggedRenderer<NCAction>(BlockRenderer, logger, logEventLevel);
        }

        // FIXME 남은 설정들도 설정화 해야 할지도?
        public IBlockPolicy<NCAction> GetPolicy(int minimumDifficulty, int maxTransactionsPerBlock) =>
            GetPolicy(
                minimumDifficulty,
                maxTransactionsPerBlock,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                authorizedMiningPolicy: AuthorizedMiningPolicy.Mainnet,
                authorizedMiningNoOpTxPolicy: AuthorizedMiningNoOpTxPolicy.Mainnet,
                permissionedMiningPolicy: PermissionedMiningPolicy.Mainnet);

        /// <summary>
        /// Gets a <see cref="BlockPolicy"/> constructed from given parameters.
        /// </summary>
        /// <param name="minimumDifficulty">The minimum difficulty that a <see cref="Block{T}"/>
        /// can have.  This is ignored for genesis blocks.</param>
        /// <param name="maxTransactionsPerBlock">The maximum number of
        /// <see cref="Transaction{T}"/>s that a <see cref="Block{T}"/> can have.</param>
        /// <param name="minTransactionsPerBlockPolicy">Used for minimum number of transactions
        /// required per block.</param>
        /// <param name="authorizedMiningPolicy">Used for authorized mining.</param>
        /// <param name="authorizedMiningNoOpTxPolicy">Used for no-op tx authorized mining.</param>
        /// <param name="permissionedMiningPolicy">Used for permissioned mining.</param>
        /// <returns>A <see cref="BlockPolicy"/> constructed from given parameters.</returns>
        internal IBlockPolicy<NCAction> GetPolicy(
            int minimumDifficulty,
            int maxTransactionsPerBlock,
            MinTransactionsPerBlockPolicy? minTransactionsPerBlockPolicy,
            AuthorizedMiningPolicy? authorizedMiningPolicy,
            AuthorizedMiningNoOpTxPolicy? authorizedMiningNoOpTxPolicy,
            PermissionedMiningPolicy? permissionedMiningPolicy)
        {
#if UNITY_EDITOR
            return new DebugPolicy();
#else
            // Basic sanity check.
            if (authorizedMiningPolicy is AuthorizedMiningPolicy amp
                && authorizedMiningNoOpTxPolicy is AuthorizedMiningNoOpTxPolicy amnotp)
            {
                if (amnotp.StartIndex < amp.StartIndex
                    || amnotp.EndIndex != amp.EndIndex
                    || amnotp.Interval != amp.Interval)
                {
                    throw new ArgumentException(
                        $"Invalid {nameof(authorizedMiningNoOpTxPolicy)} given as a subpolicy"
                            + $" for given {nameof(authorizedMiningPolicy)}.");
                }
            }
            else if (authorizedMiningPolicy is null && !(authorizedMiningNoOpTxPolicy is null))
            {
                throw new ArgumentException(
                    $"Argument {nameof(authorizedMiningNoOpTxPolicy)} cannot be null while"
                        + $" {nameof(authorizedMiningPolicy)} is null.");
            }

            var validateNextBlockTx = ValidateNextBlockTxFactory(
                authorizedMiningPolicy);
            var validateNextBlock = ValidateNextBlockFactory(
                minTransactionsPerBlockPolicy,
                authorizedMiningPolicy,
                authorizedMiningNoOpTxPolicy,
                permissionedMiningPolicy);
            var getNextBlockDifficulty = GetNextBlockDifficultyFactory(
                BlockInterval,
                DifficultyStability,
                minimumDifficulty,
                authorizedMiningPolicy);
            var getMinTransactionsPerBlock = GetMinTransactionsPerBlockFactory(
                minTransactionsPerBlockPolicy);
            var getMaxTransactionsPerBlock = GetMaxTransactionsPerBlockFactory(
                maxTransactionsPerBlock);
            var isAllowedToMine = IsAllowedToMineFactory(
                IsAuthorizedMiningBlockIndexFactory(authorizedMiningPolicy),
                IsAuthorizedToMineFactory(authorizedMiningPolicy),
                IsPermissionedMiningBlockIndexFactory(permissionedMiningPolicy),
                IsPermissionedToMineFactory(permissionedMiningPolicy));

            return new BlockPolicy(
                new RewardGold(),
                blockInterval: BlockInterval,
                difficultyStability: DifficultyStability,
                minimumDifficulty: minimumDifficulty,
                permissionedMiningPolicy: permissionedMiningPolicy,
                canonicalChainComparer: new TotalDifficultyComparer(),
#pragma warning disable LAA1002
                hashAlgorithmGetter: HashAlgorithmTable.ToHashAlgorithmGetter(),
#pragma warning restore LAA1002
                validateNextBlockTx: validateNextBlockTx,
                validateNextBlock: validateNextBlock,
                getMaxBlockBytes: GetMaxBlockBytes,
                getMinTransactionsPerBlock: getMinTransactionsPerBlock,
                getMaxTransactionsPerBlock: getMaxTransactionsPerBlock,
                getMaxTransactionsPerSignerPerBlock: GetMaxTransactionsPerSignerPerBlock,
                getNextBlockDifficulty: getNextBlockDifficulty,
                isAllowedToMine: isAllowedToMine);
#endif
        }

        public IEnumerable<IRenderer<NCAction>> GetRenderers() =>
            new IRenderer<NCAction>[] { BlockRenderer, LoggedActionRenderer };

        public static TxPolicyViolationException ValidateNextBlockTxRaw(
            BlockChain<NCAction> blockChain,
            Transaction<NCAction> transaction,
            AuthorizedMiningPolicy? authorizedMiningPolicy)
        {
            // Avoid NRE when genesis block appended
            // Here, index is the index of a prospective block that transaction
            // will be included.
            long index = blockChain.Count > 0 ? blockChain.Tip.Index : 0;

            if (transaction.Actions.Count > 1)
            {
                return new TxPolicyViolationException(
                    transaction.Id,
                    $"Transaction {transaction.Id} has too many actions: "
                        + $"{transaction.Actions.Count}");
            }
            else if (IsObsolete(transaction, index))
            {
                return new TxPolicyViolationException(
                    transaction.Id,
                    $"Transaction {transaction.Id} is obsolete.");
            }

            try
            {
                // Check if it is a no-op transaction to prove it's made by the authorized miner.
                if (IsAuthorizedMinerTransactionRaw(transaction, authorizedMiningPolicy))
                {
                    // The authorization proof has to have no actions at all.
                    return transaction.Actions.Any()
                        ? new TxPolicyViolationException(
                            transaction.Id,
                            $"Transaction {transaction.Id} by an authorized miner should not have "
                                + $"any action: {transaction.Actions.Count}")
                        : null;
                }

                // Check ActivateAccount
                if (transaction.Actions.Count == 1 &&
                    transaction.Actions.First().InnerAction is IActivateAction aa)
                {
                    return blockChain.GetState(aa.GetPendingAddress()) is Dictionary rawPending &&
                        new PendingActivationState(rawPending).Verify(aa.GetSignature())
                        ? null
                        : new TxPolicyViolationException(
                            transaction.Id,
                            $"Transaction {transaction.Id} has an invalid activate action.");
                }

                // Check admin
                if (IsAdminTransaction(blockChain, transaction))
                {
                    return null;
                }

                switch (blockChain.GetState(transaction.Signer.Derive(ActivationKey.DeriveKey)))
                {
                    case null:
                        // Fallback for pre-migration.
                        if (blockChain.GetState(ActivatedAccountsState.Address)
                            is Dictionary asDict)
                        {
                            IImmutableSet<Address> activatedAccounts =
                                new ActivatedAccountsState(asDict).Accounts;
                            return !activatedAccounts.Any() ||
                                activatedAccounts.Contains(transaction.Signer)
                                ? null
                                : new TxPolicyViolationException(
                                    transaction.Id,
                                    $"Transaction {transaction.Id} is by a signer "
                                        + $"without account activation: {transaction.Signer}");
                        }
                        return null;
                    case Bencodex.Types.Boolean _:
                        return null;
                }

                return null;
            }
            catch (InvalidSignatureException)
            {
                return new TxPolicyViolationException(
                    transaction.Id,
                    $"Transaction {transaction.Id} has invalid signautre.");
            }
            catch (IncompleteBlockStatesException)
            {
                // It can be caused during `Swarm<T>.PreloadAsync()` because it doesn't fill its
                // state right away...
                // FIXME: It should be removed after fix that Libplanet fills its state on IBD.
                // See also: https://github.com/planetarium/lib9c/pull/151#discussion_r506039478
                return null;
            }

            return null;
        }

        public static Func<BlockChain<NCAction>, Transaction<NCAction>, TxPolicyViolationException>
            ValidateNextBlockTxFactory(AuthorizedMiningPolicy? authorizedMiningPolicy)
        {
            return (blockChain, transaction) => ValidateNextBlockTxRaw(
                blockChain, transaction, authorizedMiningPolicy);
        }


        public static BlockPolicyViolationException ValidateNextBlockRaw(
            BlockChain<NCAction> blockChain,
            Block<NCAction> nextBlock,
            MinTransactionsPerBlockPolicy? minTransactionsPerBlockPolicy,
            AuthorizedMiningPolicy? authorizedMiningPolicy,
            AuthorizedMiningNoOpTxPolicy? authorizedMiningNoOpTxPolicy,
            PermissionedMiningPolicy? permissionedMiningPolicy)
        {
            // FIXME: Tx count validation should be done in libplanet, not here.
            // Should be removed once libplanet is updated.
            return ValidateTxCountPerBlockRaw(
                nextBlock,
                minTransactionsPerBlockPolicy)
                ?? ValidateMinerAuthorityRaw(
                    nextBlock,
                    authorizedMiningPolicy,
                    authorizedMiningNoOpTxPolicy)
                ?? ValidateMinerPermissionRaw(nextBlock, permissionedMiningPolicy);
        }

        public static Func<BlockChain<NCAction>, Block<NCAction>, BlockPolicyViolationException>
            ValidateNextBlockFactory(
                MinTransactionsPerBlockPolicy? minTransactionsPerBlockPolicy,
                AuthorizedMiningPolicy? authorizedMiningPolicy,
                AuthorizedMiningNoOpTxPolicy? authorizedMiningNoOpTxPolicy,
                PermissionedMiningPolicy? permissionedMiningPolicy)
        {
            return (blockChain, nextBlock) =>
                ValidateNextBlockRaw(
                    blockChain,
                    nextBlock,
                    minTransactionsPerBlockPolicy,
                    authorizedMiningPolicy,
                    authorizedMiningNoOpTxPolicy,
                    permissionedMiningPolicy);
        }

        public static int GetMaxBlockBytes(long index)
        {
            return index > 0 ? MaxBlockBytes : MaxGenesisBytes;
        }

        public static int GetMinTransactionsPerBlockRaw(
            long index, MinTransactionsPerBlockPolicy? minTransactionsPerBlockPolicy)
        {
            if (minTransactionsPerBlockPolicy is MinTransactionsPerBlockPolicy mtpbp)
            {
                if (mtpbp.IsTargetBlockIndex(index))
                {
                    return mtpbp.MinTransactionsPerBlock;
                }
            }

            return 0;
        }

        public static Func<long, int> GetMinTransactionsPerBlockFactory(
            MinTransactionsPerBlockPolicy? minTransactionsPerBlockPolicy)
        {
            return index => GetMinTransactionsPerBlockRaw(index, minTransactionsPerBlockPolicy);
        }

        public static int GetMaxTransactionsPerBlockRaw(long index, int maxTransactionsPerBlock)
        {
            return maxTransactionsPerBlock;
        }

        public static Func<long, int> GetMaxTransactionsPerBlockFactory(
            int maxTransactionsPerBlock)
        {
            return index => GetMaxTransactionsPerBlockRaw(index, maxTransactionsPerBlock);
        }

        public static int GetMaxTransactionsPerSignerPerBlock(long index)
        {
            return index >= MaxTransactionsPerSignerPerBlockHardcodedIndex
                ? MaxTransactionsPerSignerPerBlock
                : int.MaxValue;
        }

        public static long GetNextBlockDifficultyRaw(
            BlockChain<NCAction> blockChain,
            TimeSpan targetBlockInterval,
            long difficultyStability,
            long minimumDifficulty,
            AuthorizedMiningPolicy? authorizedMiningPolicy,
            Func<BlockChain<NCAction>, long> defaultAlgorithm)
        {
            long index = blockChain.Count;

            if (index < 0)
            {
                throw new InvalidBlockIndexException(
                    $"Value of {nameof(index)} must be non-negative: {index}");
            }
            else if (index <= 1)
            {
                return index == 0 ? 0 : minimumDifficulty;
            }
            // FIXME: Uninstantiated blockChain can be passed as an argument.
            // Until this is fixed, it is crucial block index is checked first.
            // Authorized minor validity is only checked for certain indices.
            else if (authorizedMiningPolicy is AuthorizedMiningPolicy amp)
            {
                long prevIndex = IsAuthorizedMiningBlockIndexRaw(
                    index - 1, authorizedMiningPolicy)
                        ? index - 2
                        : index - 1;
                long prevPrevIndex = IsAuthorizedMiningBlockIndexRaw(
                    prevIndex - 1, authorizedMiningPolicy)
                        ? prevIndex - 2
                        : prevIndex - 1;

                if (prevPrevIndex > amp.EndIndex)
                {
                    return defaultAlgorithm(blockChain);
                }
                else if (IsAuthorizedMiningBlockIndexRaw(index, authorizedMiningPolicy)
                    || prevIndex <= 1
                    || prevPrevIndex <= 1)
                {
                    return minimumDifficulty;
                }
                else
                {
                    Block<NCAction> prevBlock = blockChain[prevIndex];
                    Block<NCAction> prevPrevBlock = blockChain[prevPrevIndex];
                    TimeSpan prevTimeDiff = prevBlock.Timestamp - prevPrevBlock.Timestamp;
                    const long minimumAdjustmentMultiplier = -99;

                    long adjustmentMultiplier = Math.Max(
                        1 - ((long)prevTimeDiff.TotalMilliseconds /
                            (long)targetBlockInterval.TotalMilliseconds),
                        minimumAdjustmentMultiplier);
                    long difficultyAdjustment =
                        prevBlock.Difficulty / difficultyStability * adjustmentMultiplier;

                    long nextDifficulty = Math.Max(
                        prevBlock.Difficulty + difficultyAdjustment, minimumDifficulty);

                    return nextDifficulty;
                }
            }
            else
            {
                return defaultAlgorithm(blockChain);
            }
        }

        public static Func<BlockChain<NCAction>, long> GetNextBlockDifficultyFactory(
            TimeSpan targetBlockInterval,
            long difficultyStability,
            long minimumDifficulty,
            AuthorizedMiningPolicy? authorizedMiningPolicy)
        {
            return (blockChain) =>
                GetNextBlockDifficultyRaw(
                    blockChain: blockChain,
                    targetBlockInterval: targetBlockInterval,
                    difficultyStability: difficultyStability,
                    minimumDifficulty: minimumDifficulty,
                    authorizedMiningPolicy: authorizedMiningPolicy,
                    defaultAlgorithm: DifficultyAdjustment<NCAction>.AlgorithmFactory(
                        targetBlockInterval, difficultyStability, minimumDifficulty));
        }
    }
}
