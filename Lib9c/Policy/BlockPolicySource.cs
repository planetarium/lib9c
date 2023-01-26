using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Renderer;
using Libplanet.Action.Sys;
using Libplanet.Blocks;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Tx;
using Libplanet;
using Libplanet.Blockchain.Renderers;
using Libplanet.Consensus;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Serilog;
using Serilog.Events;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
#endif

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions;
using Lib9c.DevExtensions.Model;
#endif

namespace Nekoyume.BlockChain.Policy
{
    public partial class BlockPolicySource
    {
        public const long MinimumDifficulty = 5_000_000;

        public const long DifficultyStability = 2048;

        /// <summary>
        /// Last index in which restriction will apply.
        /// </summary>
        public const long AuthorizedMinersPolicyEndIndex = 5_716_957;

        public const long AuthorizedMinersPolicyInterval = 50;

        public const int MaxTransactionsPerBlock = 100;

        public const long V100080ObsoleteIndex = 2_448_000;

        public const long V100081ObsoleteIndex = 2_550_000;

        public const long V100083ObsoleteIndex = 2_680_000;

        public const long V100086ObsoleteIndex = 2_800_001;

        public const long V100089ObsoleteIndex = 2_908_000;

        public const long V100093ObsoleteIndex = 3_050_000;

        public const long V100095ObsoleteIndex = 3_317_632;

        public const long V100096ObsoleteIndex = 3_317_632;

        public const long V100170ObsoleteIndex = 3_810_000;

        public const long V100190ObsoleteIndex = 4_204_863;

        public const long V100193ObsoleteIndex = 3_975_929;

        public const long V100200ObsoleteIndex = 4_246_225;

        public const long V100210ObsoleteIndex = 4_366_622;

        public const long V100220ObsoleteIndex = 4_390_272;

        public const long V100230ObsoleteIndex = 4_558_559;

        public const long V100240ObsoleteIndex = 4_578_480;

        public const long V100260ObsoleteIndex = 4_596_180;

        public const long V100270ObsoleteIndex = 4_841_774;

        public const long V100282ObsoleteIndex = 4_835_445;

        public const long V100290ObsoleteIndex = 4_913_153;

        public const long V100300ObsoleteIndex = 5_150_000;

        public const long V100310ObsoleteIndex = 5_300_000;

        // NOTE:
        // V100311: ArenaSheet-2,6,Championship,5398001(start arena block index)
        public const long V100320ObsoleteIndex = 5_398_001;

        public const long V100340ObsoleteIndex = 5_800_000;

        public const long PermissionedMiningStartIndex = 2_225_500;

        public const long V100301ExecutedBlockIndex = 5_048_399L;

        public const long V100310ExecutedBlockIndex = 5_217_577L;

        public static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(8);

        public static readonly ImmutableHashSet<Address> AuthorizedMiners = new Address[]
        {
            new Address("ab1dce17dCE1Db1424BB833Af6cC087cd4F5CB6d"),
            new Address("3217f757064Cd91CAba40a8eF3851F4a9e5b4985"),
            new Address("474CB59Dea21159CeFcC828b30a8D864e0b94a6B"),
            new Address("636d187B4d434244A92B65B06B5e7da14b3810A9"),
        }.ToImmutableHashSet();

        public static readonly PublicKey ValidatorAdmin = new PublicKey(
            ByteUtil.ParseHex("02d20de0afd2231b17b3a7189c3e897796b3b31378c7b1689a6131d904bab08e5c"));

        public static readonly PrivateKey DebugValidatorKey =
            new PrivateKey("0000000000000000000000000000000000000000000000000000000000000001");

        public static readonly ValidatorSet DebugValidatorSet =
            new ValidatorSet(new List<Validator> { new Validator(DebugValidatorKey.PublicKey, BigInteger.One) });

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

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-main deployment.
        /// </summary>
        public IBlockPolicy<NCAction> GetPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet,
                validatorAdminPolicy: ValidatorAdminPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-internal deployment.
        /// </summary>
        public IBlockPolicy<NCAction> GetInternalPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Internal,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Internal,
                validatorAdminPolicy: ValidatorAdminPolicy.Internal);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-permanent-test deployment.
        /// </summary>
        public IBlockPolicy<NCAction> GetPermanentPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet,
                validatorAdminPolicy: ValidatorAdminPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance identical to the one deployed
        /// except with lower minimum difficulty for faster testing and benchmarking.
        /// </summary>
        public IBlockPolicy<NCAction> GetTestPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet,
                validatorAdminPolicy: ValidatorAdminPolicy.Test);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for networks
        /// with default options, without authorized mining and permissioned mining.
        /// </summary>
        public IBlockPolicy<NCAction> GetDefaultPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Default,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Default,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Default,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Default,
                validatorAdminPolicy: ValidatorAdminPolicy.Default);

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
        /// <param name="validatorsPolicy">Used for PBFT.</param>
        /// <returns>A <see cref="BlockPolicy"/> constructed from given parameters.</returns>
        internal IBlockPolicy<NCAction> GetPolicy(
            IVariableSubPolicy<long> maxTransactionsBytesPolicy,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy,
            IVariableSubPolicy<PublicKey> validatorAdminPolicy)
        {
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            var data = TestbedHelper.LoadData<TestbedCreateAvatar>("TestbedCreateAvatar");
            return new DebugPolicy();
#else
            maxTransactionsBytesPolicy = maxTransactionsBytesPolicy
                ?? MaxTransactionsBytesPolicy.Default;
            minTransactionsPerBlockPolicy = minTransactionsPerBlockPolicy
                ?? MinTransactionsPerBlockPolicy.Default;
            maxTransactionsPerBlockPolicy = maxTransactionsPerBlockPolicy
                ?? MaxTransactionsPerBlockPolicy.Default;
            maxTransactionsPerSignerPerBlockPolicy = maxTransactionsPerSignerPerBlockPolicy
                ?? MaxTransactionsPerSignerPerBlockPolicy.Default;
            validatorAdminPolicy = validatorAdminPolicy
                ?? ValidatorAdminPolicy.Default;

            Func<BlockChain<NCAction>, Transaction<NCAction>, TxPolicyViolationException> validateNextBlockTx =
                (blockChain, transaction) => ValidateNextBlockTxRaw(
                    blockChain, transaction);
            Func<BlockChain<NCAction>, Block<NCAction>, BlockPolicyViolationException> validateNextBlock =
                (blockChain, block) => ValidateNextBlockRaw(
                    blockChain,
                    block,
                    maxTransactionsBytesPolicy,
                    minTransactionsPerBlockPolicy,
                    maxTransactionsPerBlockPolicy,
                    maxTransactionsPerSignerPerBlockPolicy,
                    validatorAdminPolicy);
            Func<Address, long, bool> isAllowedToMine = (address, index) => true;

            // FIXME: Slight inconsistency due to pre-existing delegate.
            return new BlockPolicy(
                new RewardGold(),
                blockInterval: BlockInterval,
                validateNextBlockTx: validateNextBlockTx,
                validateNextBlock: validateNextBlock,
                getMaxTransactionsBytes: maxTransactionsBytesPolicy.Getter,
                getMinTransactionsPerBlock: minTransactionsPerBlockPolicy.Getter,
                getMaxTransactionsPerBlock: maxTransactionsPerBlockPolicy.Getter,
                getMaxTransactionsPerSignerPerBlock: maxTransactionsPerSignerPerBlockPolicy.Getter,
                isAllowedToMine: isAllowedToMine);
#endif
        }

        public IEnumerable<IRenderer<NCAction>> GetRenderers() =>
            new IRenderer<NCAction>[] { BlockRenderer, LoggedActionRenderer };

        internal static TxPolicyViolationException ValidateNextBlockTxRaw(
            BlockChain<NCAction> blockChain,
            Transaction<NCAction> transaction)
        {
            // Avoid NRE when genesis block appended
            // Here, index is the index of a prospective block that transaction
            // will be included.
            long index = blockChain.Count > 0 ? blockChain.Tip.Index : 0;

            if (transaction.Actions.Count > 1)
            {
                return new TxPolicyViolationException(
                    $"Transaction {transaction.Id} has too many actions: " +
                    $"{transaction.Actions.Count}",
                    transaction.Id);
            }
            else if (IsObsolete(transaction, index))
            {
                return new TxPolicyViolationException(
                    $"Transaction {transaction.Id} is obsolete.",
                    transaction.Id);
            }

            try
            {
                // Check ActivateAccount
                if (transaction.CustomActions is { } customActions &&
                    customActions.Count == 1 &&
                    customActions.First().InnerAction is IActivateAction aa)
                {
                    return transaction.Nonce == 0 &&
                        blockChain.GetState(aa.GetPendingAddress()) is Dictionary rawPending &&
                        new PendingActivationState(rawPending).Verify(aa.GetSignature())
                        ? null
                        : new TxPolicyViolationException(
                            $"Transaction {transaction.Id} has an invalid activate action.",
                            transaction.Id);
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
                                    $"Transaction {transaction.Id} is by a signer " +
                                    $"without account activation: {transaction.Signer}",
                                    transaction.Id);
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
                    $"Transaction {transaction.Id} has invalid signautre.",
                    transaction.Id);
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

        internal static BlockPolicyViolationException ValidateNextBlockRaw(
            BlockChain<NCAction> blockChain,
            Block<NCAction> nextBlock,
            IVariableSubPolicy<long> maxTransactionsBytesPolicy,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy,
            IVariableSubPolicy<PublicKey> validatorAdminPolicy)
        {
            if (ValidateTransactionsBytesRaw(
                nextBlock,
                maxTransactionsBytesPolicy) is InvalidBlockBytesLengthException ibble)
            {
                return ibble;
            }
            else if (ValidateTxCountPerBlockRaw(
                nextBlock,
                minTransactionsPerBlockPolicy,
                maxTransactionsPerBlockPolicy) is InvalidBlockTxCountException ibtce)
            {
                return ibtce;
            }
            else if (ValidateTxCountPerSignerPerBlockRaw(
                nextBlock,
                maxTransactionsPerSignerPerBlockPolicy) is InvalidBlockTxCountPerSignerException ibtcpse)
            {
                return ibtcpse;
            }

            else if (ValidateSetValidatorActionRaw(
                blockChain,
                nextBlock,
                validatorAdminPolicy) is BlockPolicyViolationException bpve)
            {
                return bpve;
            }

            return null;
        }
    }
}
