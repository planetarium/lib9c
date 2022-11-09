using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Renderer;
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

        public const long PermissionedMiningStartIndex = 2_225_500;


        public static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(8);

        public static readonly ImmutableHashSet<Address> AuthorizedMiners = new Address[]
        {
            new Address("ab1dce17dCE1Db1424BB833Af6cC087cd4F5CB6d"),
            new Address("3217f757064Cd91CAba40a8eF3851F4a9e5b4985"),
            new Address("474CB59Dea21159CeFcC828b30a8D864e0b94a6B"),
            new Address("636d187B4d434244A92B65B06B5e7da14b3810A9"),
        }.ToImmutableHashSet();

        public static readonly ValidatorSet Validators = new ValidatorSet(new List<PublicKey>()
        {
            new PublicKey(ByteUtil.ParseHex("03c5053b7bc6f1718ef95442f508f0f44196ef36b2dd712768828daa4c25608efe")),
            new PublicKey(ByteUtil.ParseHex("03c43a4bccc99dca6206cf6d6070f2eaa72a544e503a70318cf1ac5db94fcb30b7")),
            new PublicKey(ByteUtil.ParseHex("03b2996c69e8064953bbaeac29d5043225607a1db8a3fd359863b9de440d002ee6")),
            new PublicKey(ByteUtil.ParseHex("034749ddaaec8548ac1c7d402611b9270aad07b861a0705944ed7a9f56be4ecc65")),
        });

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
                minimumDifficulty: MinimumDifficulty,
                maxBlockBytesPolicy: MaxBlockBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet,
                authorizedMinersPolicy: null,
                permissionedMinersPolicy: null,
                validatorsPolicy: ValidatorsPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-internal deployment.
        /// </summary>
        public IBlockPolicy<NCAction> GetInternalPolicy() =>
            GetPolicy(
                minimumDifficulty: MinimumDifficulty,
                maxBlockBytesPolicy: MaxBlockBytesPolicy.Internal,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Internal,
                authorizedMinersPolicy: AuthorizedMinersPolicy.Mainnet,
                permissionedMinersPolicy: PermissionedMinersPolicy.Mainnet,
                validatorsPolicy: ValidatorsPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-permanent-test deployment.
        /// </summary>
        public IBlockPolicy<NCAction> GetPermanentPolicy() =>
            GetPolicy(
                minimumDifficulty: DifficultyStability,
                maxBlockBytesPolicy: MaxBlockBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet,
                authorizedMinersPolicy: AuthorizedMinersPolicy.Permanent,
                permissionedMinersPolicy: PermissionedMinersPolicy.Permanent,
                validatorsPolicy: ValidatorsPolicy.Permanent);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance identical to the one deployed
        /// except with lower minimum difficulty for faster testing and benchmarking.
        /// </summary>
        public IBlockPolicy<NCAction> GetTestPolicy() =>
            GetPolicy(
                minimumDifficulty: DifficultyStability,
                maxBlockBytesPolicy: MaxBlockBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet,
                authorizedMinersPolicy: AuthorizedMinersPolicy.Mainnet,
                permissionedMinersPolicy: PermissionedMinersPolicy.Mainnet,
                validatorsPolicy: ValidatorsPolicy.Test);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for networks
        /// with default options, without authorized mining and permissioned mining.
        /// </summary>
        public IBlockPolicy<NCAction> GetDefaultPolicy() =>
            GetPolicy(
                minimumDifficulty: DifficultyStability,
                maxBlockBytesPolicy: MaxBlockBytesPolicy.Default,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Default,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Default,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Default,
                authorizedMinersPolicy: AuthorizedMinersPolicy.Default,
                permissionedMinersPolicy: PermissionedMinersPolicy.Default,
                validatorsPolicy: ValidatorsPolicy.Default);

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
        /// <param name="authorizedMinersPolicy">Used for authorized mining.</param>
        /// <param name="permissionedMinersPolicy">Used for permissioned mining.</param>
        /// <param name="validatorsPolicy">Used for PBFT.</param>
        /// <returns>A <see cref="BlockPolicy"/> constructed from given parameters.</returns>
        internal IBlockPolicy<NCAction> GetPolicy(
            long minimumDifficulty,
            IVariableSubPolicy<long> maxBlockBytesPolicy,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> authorizedMinersPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> permissionedMinersPolicy,
            IVariableSubPolicy<ValidatorSet> validatorsPolicy)
        {
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            var data = TestbedHelper.LoadData<TestbedCreateAvatar>("TestbedCreateAvatar");
             return new DebugPolicy(data.BlockDifficulty);
#else
            maxBlockBytesPolicy = maxBlockBytesPolicy
                ?? MaxBlockBytesPolicy.Default;
            minTransactionsPerBlockPolicy = minTransactionsPerBlockPolicy
                ?? MinTransactionsPerBlockPolicy.Default;
            maxTransactionsPerBlockPolicy = maxTransactionsPerBlockPolicy
                ?? MaxTransactionsPerBlockPolicy.Default;
            maxTransactionsPerSignerPerBlockPolicy = maxTransactionsPerSignerPerBlockPolicy
                ?? MaxTransactionsPerSignerPerBlockPolicy.Default;
            authorizedMinersPolicy = authorizedMinersPolicy
                ?? AuthorizedMinersPolicy.Default;
            permissionedMinersPolicy = permissionedMinersPolicy
                ?? PermissionedMinersPolicy.Default;
            validatorsPolicy = validatorsPolicy
                ?? ValidatorsPolicy.Default;


            // FIXME: Ad hoc solution to poorly defined tx validity.
            ImmutableHashSet<Address> allAuthorizedMiners =
                authorizedMinersPolicy.SpannedSubPolicies
                    .Select(spannedSubPolicy => spannedSubPolicy.Value)
#pragma warning disable LAA1002
                    .Aggregate(
                        authorizedMinersPolicy.DefaultValue,
                        (union, next) => union.Union(next));
#pragma warning restore LAA1002

            Func<BlockChain<NCAction>, Transaction<NCAction>, TxPolicyViolationException> validateNextBlockTx =
                (blockChain, transaction) => ValidateNextBlockTxRaw(
                    blockChain, transaction, allAuthorizedMiners);
            Func<BlockChain<NCAction>, Block<NCAction>, BlockPolicyViolationException> validateNextBlock =
                (blockChain, block) => ValidateNextBlockRaw(
                    blockChain,
                    block,
                    maxBlockBytesPolicy,
                    minTransactionsPerBlockPolicy,
                    maxTransactionsPerBlockPolicy,
                    maxTransactionsPerSignerPerBlockPolicy,
                    authorizedMinersPolicy,
                    permissionedMinersPolicy);
            Func<Address, long, bool> isAllowedToMine = (address, index) => true;

            // FIXME: Slight inconsistency due to pre-existing delegate.
            return new BlockPolicy(
                new RewardGold(),
                blockInterval: BlockInterval,
                validateNextBlockTx: validateNextBlockTx,
                validateNextBlock: validateNextBlock,
                getMaxTransactionsBytes: maxBlockBytesPolicy.Getter,
                getMinTransactionsPerBlock: minTransactionsPerBlockPolicy.Getter,
                getMaxTransactionsPerBlock: maxTransactionsPerBlockPolicy.Getter,
                getMaxTransactionsPerSignerPerBlock: maxTransactionsPerSignerPerBlockPolicy.Getter,
                isAllowedToMine: isAllowedToMine,
                getValidatorSet: validatorsPolicy.Getter);
#endif
        }

        public IEnumerable<IRenderer<NCAction>> GetRenderers() =>
            new IRenderer<NCAction>[] { BlockRenderer, LoggedActionRenderer };

        internal static TxPolicyViolationException ValidateNextBlockTxRaw(
            BlockChain<NCAction> blockChain,
            Transaction<NCAction> transaction,
            ImmutableHashSet<Address> allAuthorizedMiners)
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
                // Check if it is a no-op transaction to prove it's made by the authorized miner.
                if (IsAuthorizedMinerTransactionRaw(transaction, allAuthorizedMiners))
                {
                    // FIXME: This works under a strong assumption that any miner that was ever
                    // in a set of authorized miners can only create transactions without
                    // any actions.
                    return transaction.Actions.Any()
                        ? new TxPolicyViolationException(
                            $"Transaction {transaction.Id} by an authorized miner should not " +
                            $"have any action: {transaction.Actions.Count}",
                            transaction.Id)
                        : null;
                }

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
            IVariableSubPolicy<long> maxBlockBytesPolicy,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> authorizedMinersPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> permissionedMinersPolicy)
        {
            if (ValidateBlockBytesRaw(
                nextBlock,
                maxBlockBytesPolicy) is InvalidBlockBytesLengthException ibble)
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
            else
            {
                if (nextBlock.Index == 0)
                {
                    return null;
                }
                else if (authorizedMinersPolicy.IsTargetIndex(nextBlock.Index))
                {
                    return ValidateMinerAuthorityRaw(
                        nextBlock,
                        authorizedMinersPolicy);
                }
                else if (permissionedMinersPolicy.IsTargetIndex(nextBlock.Index))
                {
                    return ValidateMinerPermissionRaw(
                        nextBlock,
                        permissionedMinersPolicy);
                }
            }

            return null;
        }
    }
}
