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

        public static readonly ImmutableList<PublicKey> Validators = new List<PublicKey>
        {
            new PublicKey(ByteUtil.ParseHex("03c5053b7bc6f1718ef95442f508f0f44196ef36b2dd712768828daa4c25608efe")), // validator01
            new PublicKey(ByteUtil.ParseHex("03c43a4bccc99dca6206cf6d6070f2eaa72a544e503a70318cf1ac5db94fcb30b7")), // validator02
            new PublicKey(ByteUtil.ParseHex("03b2996c69e8064953bbaeac29d5043225607a1db8a3fd359863b9de440d002ee6")), // validator03
            new PublicKey(ByteUtil.ParseHex("034749ddaaec8548ac1c7d402611b9270aad07b861a0705944ed7a9f56be4ecc65")), // validator04
            new PublicKey(ByteUtil.ParseHex("02b73af503b82c5beffb9fdc4f3498a507abd0bfdee5efab2a3edd11eebac02069")), // validator05
            new PublicKey(ByteUtil.ParseHex("03c85cedc87085f79e081680397a3983528e20fb042a9ed2bb1090ff04955728fb")), // validator06
            new PublicKey(ByteUtil.ParseHex("0324fc4511544ad3173b75e60343a30ca5042a65a30ea45d607452da3e6a42d554")), // validator07
            new PublicKey(ByteUtil.ParseHex("02b0de184d1908a47225a4a594aa5334551c17db05ec32ec166e006305fbf136c6")), // validator08
            new PublicKey(ByteUtil.ParseHex("028a9963ffe2ebbc016a2c36fa799304f37ff3dd5ebb9b70cc0926b08fec3ed457")), // validator09
            new PublicKey(ByteUtil.ParseHex("02fab92bb7555f44d545436876e114abe73975e3e2565883032caeb459a0462e43")), // validator10
            new PublicKey(ByteUtil.ParseHex("032ee2db53722995dda649de7bea748978d4255f9eed1dd298d620af4e42f0ef2f")), // validator11
            new PublicKey(ByteUtil.ParseHex("02fd07205b908c4c65758fddf1a2f1b38c6f76b3ef2b8546a7370dd22ad34d6476")), // validator12
            new PublicKey(ByteUtil.ParseHex("032c4e43ade2c60ab0cc3e6bd75289b89e3c5160bc9a7519787714cb27478f1467")), // validator13
            new PublicKey(ByteUtil.ParseHex("03ea2a447cb66028cb33bfea203b031c128e58dd8fccb642148cd4bbbeef05f373")), // validator14
            new PublicKey(ByteUtil.ParseHex("03e429ccdb8739104636509b2231665683bf91749521ddc09cf3710709b2a0764b")), // validator15
            new PublicKey(ByteUtil.ParseHex("02cf138bb22ee06df988dc7019534d919e5d70e31bca7e3cf4e663064a92f20dc7")), // validator16
            new PublicKey(ByteUtil.ParseHex("033b926e3ca5f62ea6458e2ce35063d257063ea9d3766ed9c53c2d38fd14db9540")), // validator17
            new PublicKey(ByteUtil.ParseHex("02dd45e04cddef18607a2eff4662c4157a38177c422200914d521216bc0dc8f7dc")), // validator18
            new PublicKey(ByteUtil.ParseHex("03ebbbc4f1ae2ed659648ca154e3563a520d70054ba16fff63fb27b7526d88b4e3")), // validator19
            new PublicKey(ByteUtil.ParseHex("027dc1a98fde710b833f54df8b759b139b3911968a85354302037ad995990c8cb8")), // validator20
        }.ToImmutableList();

        public static readonly ImmutableList<PublicKey> ExternalValidators = new List<PublicKey>
        {
            new PublicKey(ByteUtil.ParseHex("035206890fd8736555ce667672b8183efacd9bf840b6c5ee8eb7f5703e7bddf38c")), // FioX
            new PublicKey(ByteUtil.ParseHex("02d25568df5edd893dae2fadd02cf9fc3a281642553eaceab39ec15e01c990200f")), // Se2on
            new PublicKey(ByteUtil.ParseHex("02dfdc95a830bcc4f23953964916e94593beef325821589c03bec5c22463e56240")), // TarogStar
            new PublicKey(ByteUtil.ParseHex("02a553be962eebc60d7a5ad534990a8eb73f400ae9006b2e5b91a8622184a1f4a3")), // OMS
            new PublicKey(ByteUtil.ParseHex("02787d037d09fb6c3c2b4a7aee94b4e06dbf10543920575e2f96d74ffabe55415c")), // RTDragon
        }.ToImmutableList();

        public static readonly ValidatorSet ValidatorSet01 =
            new ValidatorSet(Validators.Take(7).ToList());                                              // 01 ~ 07

        public static readonly ValidatorSet ValidatorSet02 =
            new ValidatorSet(Validators.Skip(2).Take(7).Concat(ExternalValidators.Take(1)).ToList());   // 03 ~ 09 + 1 External

        public static readonly ValidatorSet ValidatorSet03 =
            new ValidatorSet(Validators.Take(9).Concat(ExternalValidators.Take(3)).ToList());           // 01 ~ 09 + 3 External

        public static readonly ValidatorSet ValidatorSet04 =
            new ValidatorSet(Validators.Take(13).Concat(ExternalValidators.Take(3)).ToList());          // 01 ~ 13 + 3 External

        public static readonly ValidatorSet ValidatorSet05 =
            new ValidatorSet(Validators.Take(17).Concat(ExternalValidators.Take(5)).ToList());          // 01 ~ 17 + 5 External

        public static readonly PrivateKey DebugValidatorKey =
            new PrivateKey("0000000000000000000000000000000000000000000000000000000000000001");

        public static readonly ValidatorSet DebugValidatorSet =
            new ValidatorSet(new List<PublicKey> { DebugValidatorKey.PublicKey });

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
                validatorsPolicy: ValidatorsPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-internal deployment.
        /// </summary>
        public IBlockPolicy<NCAction> GetInternalPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Internal,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Internal,
                validatorsPolicy: ValidatorsPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-permanent-test deployment.
        /// </summary>
        public IBlockPolicy<NCAction> GetPermanentPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet,
                validatorsPolicy: ValidatorsPolicy.Permanent);

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
                validatorsPolicy: ValidatorsPolicy.Test);

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
        /// <param name="validatorsPolicy">Used for PBFT.</param>
        /// <returns>A <see cref="BlockPolicy"/> constructed from given parameters.</returns>
        internal IBlockPolicy<NCAction> GetPolicy(
            IVariableSubPolicy<long> maxTransactionsBytesPolicy,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy,
            IVariableSubPolicy<ValidatorSet> validatorsPolicy)
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
            validatorsPolicy = validatorsPolicy
                ?? ValidatorsPolicy.Default;

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
                    maxTransactionsPerSignerPerBlockPolicy);
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
                isAllowedToMine: isAllowedToMine,
                getValidatorSet: validatorsPolicy.Getter);
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
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy)
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

            return null;
        }
    }
}
