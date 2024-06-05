namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Numerics;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Lib9c.Renderers;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Consensus;
    using Libplanet.Types.Tx;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Loader;
    using Nekoyume.Blockchain.Policy;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class BlockPolicyTest
    {
        private readonly PrivateKey _privateKey;
        private readonly Currency _currency;

        public BlockPolicyTest()
        {
            _privateKey = new PrivateKey();
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, _privateKey.Address);
#pragma warning restore CS0618
        }

        [Fact]
        public void ValidateNextBlockTx()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.Address;

            var blockPolicySource = new BlockPolicySource();
            IBlockPolicy policy = blockPolicySource.GetPolicy(null, null, null, null);
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            Block genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                initialValidators: new Dictionary<PublicKey, BigInteger>
                { { adminPrivateKey.PublicKey, BigInteger.One } }
            );
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockChain = BlockChain.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new ActionEvaluator(
                    new PolicyActionsRegistry(
                        beginBlockActionsGetter: _ => policy.BeginBlockActions,
                        endBlockActionsGetter: _ => policy.EndBlockActions,
                        beginTxActionsGetter: _ => policy.BeginTxActions,
                        endTxActionsGetter: _ => policy.EndTxActions),
                    stateStore: stateStore,
                    actionTypeLoader: new NCActionLoader()
                ),
                renderers: new[] { new BlockRenderer() }
            );
            Transaction txByStranger =
                Transaction.Create(
                    0,
                    new PrivateKey(),
                    genesis.Hash,
                    Array.Empty<IValue>()
                );

            // New private key which is not in activated addresses list is blocked.
            Assert.NotNull(policy.ValidateNextBlockTx(blockChain, txByStranger));

            var newActivatedPrivateKey = new PrivateKey();
            var newActivatedAddress = newActivatedPrivateKey.Address;

            // Activate with admin account.
            blockChain.MakeTransaction(
                adminPrivateKey,
                new ActionBase[] { new AddActivatedAccount(newActivatedAddress) }
            );
            Block block = blockChain.ProposeBlock(adminPrivateKey);
            blockChain.Append(block, GenerateBlockCommit(block, adminPrivateKey));

            Transaction txByNewActivated =
                Transaction.Create(
                    0,
                    newActivatedPrivateKey,
                    genesis.Hash,
                    Array.Empty<IValue>()
                );

            // Test success because the key is activated.
            Assert.Null(policy.ValidateNextBlockTx(blockChain, txByNewActivated));

            var singleAction = new ActionBase[]
            {
                new DailyReward(),
            };
            var manyActions = new ActionBase[]
            {
                new DailyReward(),
                new DailyReward(),
            };
            Transaction txWithSingleAction =
                Transaction.Create(
                    0,
                    newActivatedPrivateKey,
                    genesis.Hash,
                    singleAction.ToPlainValues()
                );
            Transaction txWithManyActions =
                Transaction.Create(
                    0,
                    newActivatedPrivateKey,
                    genesis.Hash,
                    manyActions.ToPlainValues()
                );

            // Transaction with more than two actions is rejected.
            Assert.Null(policy.ValidateNextBlockTx(blockChain, txWithSingleAction));
            Assert.NotNull(policy.ValidateNextBlockTx(blockChain, txWithManyActions));
        }

        // FIXME restore this test
        // [Fact]
        // public void ValidateNextBlockTx_Mead()
        // {
        //     var adminPrivateKey = new PrivateKey();
        //     var adminAddress = adminPrivateKey.Address;
        //     var blockPolicySource = new BlockPolicySource();
        //     var actionTypeLoader = new NCActionLoader();
        //     IBlockPolicy policy = blockPolicySource.GetPolicy(null, null, null, null);
        //     IStagePolicy stagePolicy = new VolatileStagePolicy();
        //     var mint = new PrepareRewardAssets
        //     {
        //         RewardPoolAddress = adminAddress,
        //         Assets = new List<FungibleAssetValue>
        //         {
        //             1 * Currencies.Mead,
        //         },
        //     };
        //     var mint2 = new PrepareRewardAssets
        //     {
        //         RewardPoolAddress = MeadConfig.PatronAddress,
        //         Assets = new List<FungibleAssetValue>
        //         {
        //             1 * Currencies.Mead,
        //         },
        //     };
        //     Block genesis = MakeGenesisBlock(
        //         adminAddress,
        //         ImmutableHashSet<Address>.Empty,
        //         initialValidators: new Dictionary<PublicKey, BigInteger>
        //             { { adminPrivateKey.PublicKey, BigInteger.One } },
        //         actionBases: new[] { mint, mint2 },
        //         privateKey: adminPrivateKey
        //     );
        //     using var store = new DefaultStore(null);
        //     using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
        //     var blockChain = BlockChain.Create(
        //         policy,
        //         stagePolicy,
        //         store,
        //         stateStore,
        //         genesis,
        //         new ActionEvaluator(
        //             policyBlockActionGetter: _ => policy.BlockAction,
        //             stateStore: stateStore,
        //             actionTypeLoader: new NCActionLoader()
        //         ),
        //         renderers: new[] { new BlockRenderer() }
        //     );
        //     Assert.Equal(
        //         1 * Currencies.Mead,
        //         blockChain
        //             .GetWorldState()
        //             .GetAccountState(ReservedAddresses.LegacyAccount)
        //             .GetBalance(adminAddress, Currencies.Mead));
        //     Assert.Equal(
        //         1 * Currencies.Mead,
        //         blockChain
        //             .GetWorldState()
        //             .GetAccountState(ReservedAddresses.LegacyAccount)
        //             .GetBalance(MeadConfig.PatronAddress, Currencies.Mead));
        //     var action = new DailyReward
        //     {
        //         avatarAddress = adminAddress,
        //     };
        //
        //     Transaction txEmpty =
        //         Transaction.Create(
        //             0,
        //             adminPrivateKey,
        //             genesis.Hash,
        //             Array.Empty<IValue>()
        //         );
        //     Assert.IsType<TxPolicyViolationException>(BlockPolicySource.ValidateNextBlockTxRaw(blockChain, actionTypeLoader, txEmpty));
        //
        //     Transaction txByAdmin =
        //         Transaction.Create(
        //             0,
        //             adminPrivateKey,
        //             genesis.Hash,
        //             new ActionBase[] { action, action }.ToPlainValues()
        //         );
        //     Assert.IsType<TxPolicyViolationException>(BlockPolicySource.ValidateNextBlockTxRaw(blockChain, actionTypeLoader, txByAdmin));
        //
        //     Transaction txByStranger =
        //         Transaction.Create(
        //             0,
        //             new PrivateKey(),
        //             genesis.Hash,
        //             new ActionBase[] { action }.ToPlainValues()
        //         );
        //     Assert.IsType<TxPolicyViolationException>(BlockPolicySource.ValidateNextBlockTxRaw(blockChain, actionTypeLoader, txByStranger));
        //
        //     Transaction txByAdmin2 =
        //         Transaction.Create(
        //             1,
        //             adminPrivateKey,
        //             genesis.Hash,
        //             gasLimit: 1,
        //             maxGasPrice: new FungibleAssetValue(Currencies.Mead, 10, 10),
        //             actions: new ActionBase[] { action }.ToPlainValues()
        //         );
        //     Assert.IsType<TxPolicyViolationException>(BlockPolicySource.ValidateNextBlockTxRaw(blockChain, actionTypeLoader, txByAdmin2));
        //
        //     Transaction txByAdmin3 =
        //         Transaction.Create(
        //             2,
        //             adminPrivateKey,
        //             genesis.Hash,
        //             gasLimit: 1,
        //             maxGasPrice: new FungibleAssetValue(Currencies.Mead, 0, 0),
        //             actions: new ActionBase[] { action }.ToPlainValues()
        //         );
        //     Assert.Null(BlockPolicySource.ValidateNextBlockTxRaw(blockChain, actionTypeLoader, txByAdmin3));
        // }
        [Fact]
        public void BlockCommitFromNonValidator()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.Address;
            var nonValidator = new PrivateKey();

            var blockPolicySource = new BlockPolicySource();
            IBlockPolicy policy = blockPolicySource.GetPolicy(null, null, null, null);
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            Block genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                initialValidators: new Dictionary<PublicKey, BigInteger>
                { { adminPrivateKey.PublicKey, BigInteger.One } }
            );
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockChain = BlockChain.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new ActionEvaluator(
                    new PolicyActionsRegistry(
                        beginBlockActionsGetter: _ => policy.BeginBlockActions,
                        endBlockActionsGetter: _ => policy.EndBlockActions,
                        beginTxActionsGetter: _ => policy.BeginTxActions,
                        endTxActionsGetter: _ => policy.EndTxActions),
                    stateStore: stateStore,
                    actionTypeLoader: new NCActionLoader()
                ),
                renderers: new[] { new BlockRenderer() }
            );
            blockChain.MakeTransaction(
                adminPrivateKey,
                new ActionBase[] { new AddActivatedAccount(adminPrivateKey.Address) }
            );
            Block block1 = blockChain.ProposeBlock(adminPrivateKey);
            Assert.Throws<InvalidBlockCommitException>(
                () => blockChain.Append(block1, GenerateBlockCommit(block1, nonValidator)));
        }

        [Fact]
        public void MustNotIncludeBlockActionAtTransaction()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.Address;
            var authorizedMinerPrivateKey = new PrivateKey();

            (ActivationKey ak, PendingActivationState ps) = ActivationKey.Create(
                new PrivateKey(),
                new byte[] { 0x00, 0x01 }
            );

            var blockPolicySource = new BlockPolicySource();
            IBlockPolicy policy = blockPolicySource.GetPolicy(
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: null,
                maxTransactionsPerSignerPerBlockPolicy: null);
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            Block genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                new AuthorizedMinersState(
                    new[] { authorizedMinerPrivateKey.Address },
                    5,
                    10
                ),
                pendingActivations: new[] { ps }
            );
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var actionLoader = new NCActionLoader();
            var blockChain = BlockChain.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new ActionEvaluator(
                    new PolicyActionsRegistry(
                        beginBlockActionsGetter: _ => policy.BeginBlockActions,
                        endBlockActionsGetter: _ => policy.EndBlockActions,
                        beginTxActionsGetter: _ => policy.BeginTxActions,
                        endTxActionsGetter: _ => policy.EndTxActions),
                    stateStore: stateStore,
                    actionTypeLoader: actionLoader
                ),
                renderers: new[] { new BlockRenderer() }
            );

            var unloadableAction = blockChain.MakeTransaction(
                adminPrivateKey, new ActionBase[] { new RewardGold() }).Actions[0];
            Assert.Throws<InvalidActionException>(() =>
                actionLoader.LoadAction(blockChain.Tip.Index, unloadableAction));
        }

        [Fact]
        public void EarnMiningGoldWhenSuccessMining()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.Address;
            var authorizedMinerPrivateKey = new PrivateKey();

            (ActivationKey ak, PendingActivationState ps) = ActivationKey.Create(
                new PrivateKey(),
                new byte[] { 0x00, 0x01 }
            );

            var blockPolicySource = new BlockPolicySource();
            IBlockPolicy policy = blockPolicySource.GetPolicy(
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: null,
                maxTransactionsPerSignerPerBlockPolicy: null);
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            Block genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                new AuthorizedMinersState(
                    new[] { authorizedMinerPrivateKey.Address },
                    5,
                    10
                ),
                new Dictionary<PublicKey, BigInteger> { { adminPrivateKey.PublicKey, BigInteger.One } },
                pendingActivations: new[] { ps }
            );

            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockChain = BlockChain.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new ActionEvaluator(
                    new PolicyActionsRegistry(
                        beginBlockActionsGetter: _ => policy.BeginBlockActions,
                        endBlockActionsGetter: _ => policy.EndBlockActions,
                        beginTxActionsGetter: _ => policy.BeginTxActions,
                        endTxActionsGetter: _ => policy.EndTxActions),
                    stateStore: stateStore,
                    actionTypeLoader: new NCActionLoader()
                ),
                renderers: new[] { new BlockRenderer() }
            );

            blockChain.MakeTransaction(
                adminPrivateKey,
                new ActionBase[] { new DailyReward(), }
            );

            Block block = blockChain.ProposeBlock(adminPrivateKey);
            blockChain.Append(block, GenerateBlockCommit(block, adminPrivateKey));
            FungibleAssetValue actualBalance = blockChain
                .GetNextWorldState()
                .GetBalance(adminAddress, _currency);
            FungibleAssetValue expectedBalance = new FungibleAssetValue(_currency, 10, 0);
            Assert.True(expectedBalance.Equals(actualBalance));
        }

        [Fact]
        public void ValidateNextBlockWithManyTransactions()
        {
            var adminPrivateKey = new PrivateKey();
            var adminPublicKey = adminPrivateKey.PublicKey;
            var blockPolicySource = new BlockPolicySource();
            IBlockPolicy policy = blockPolicySource.GetPolicy(
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(0, null, null, 10)),
                maxTransactionsPerSignerPerBlockPolicy: null);
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            Block genesis =
                MakeGenesisBlock(
                    adminPublicKey.Address,
                    ImmutableHashSet<Address>.Empty,
                    initialValidators: new Dictionary<PublicKey, BigInteger>
                    { { adminPrivateKey.PublicKey, BigInteger.One } });

            using var store = new DefaultStore(null);
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var actionEvaluator = new ActionEvaluator(
                new PolicyActionsRegistry(
                    beginBlockActionsGetter: _ => policy.BeginBlockActions,
                    endBlockActionsGetter: _ => policy.EndBlockActions,
                    beginTxActionsGetter: _ => policy.BeginTxActions,
                    endTxActionsGetter: _ => policy.EndTxActions),
                stateStore: stateStore,
                actionTypeLoader: new NCActionLoader()
            );
            var blockChain = BlockChain.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                actionEvaluator
            );

            int nonce = 0;
            List<Transaction> GenerateTransactions(int count)
            {
                var list = new List<Transaction>();
                for (int i = 0; i < count; i++)
                {
                    list.Add(Transaction.Create(
                        nonce++,
                        adminPrivateKey,
                        genesis.Hash,
                        Array.Empty<IValue>()
                    ));
                }

                return list;
            }

            Assert.Equal(1, blockChain.Count);
            var txs = GenerateTransactions(5).OrderBy(tx => tx.Id).ToList();
            var preEvalBlock1 = new BlockContent(
                new BlockMetadata(
                    index: 1,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: null),
                transactions: txs).Propose();
            Block block1 = EvaluateAndSign(store, actionEvaluator, preEvalBlock1, adminPrivateKey);
            blockChain.Append(block1, GenerateBlockCommit(block1, adminPrivateKey));
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));
            txs = GenerateTransactions(10).OrderBy(tx => tx.Id).ToList();
            PreEvaluationBlock preEvalBlock2 = new BlockContent(
                new BlockMetadata(
                    index: 2,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: GenerateBlockCommit(blockChain.Tip, adminPrivateKey)),
                transactions: txs).Propose();
            Block block2 = EvaluateAndSign(store, actionEvaluator, preEvalBlock2, adminPrivateKey);
            blockChain.Append(block2, GenerateBlockCommit(block2, adminPrivateKey));
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block2.Hash));
            txs = GenerateTransactions(11).OrderBy(tx => tx.Id).ToList();
            PreEvaluationBlock preEvalBlock3 = new BlockContent(
                new BlockMetadata(
                    index: 3,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: GenerateBlockCommit(blockChain.Tip, adminPrivateKey)),
                transactions: txs).Propose();
            Block block3 = EvaluateAndSign(store, actionEvaluator, preEvalBlock3, adminPrivateKey);
            Assert.Throws<InvalidBlockTxCountException>(
                () => blockChain.Append(block3, GenerateBlockCommit(block3, adminPrivateKey)));
            Assert.Equal(3, blockChain.Count);
            Assert.False(blockChain.ContainsBlock(block3.Hash));
        }

        [Fact]
        public void ValidateNextBlockWithManyTransactionsPerSigner()
        {
            var adminPrivateKey = new PrivateKey();
            var adminPublicKey = adminPrivateKey.PublicKey;
            var blockPolicySource = new BlockPolicySource();
            IBlockPolicy policy = blockPolicySource.GetPolicy(
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(0, null, null, 10)),
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(2, null, null, 5)));
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            Block genesis =
                MakeGenesisBlock(
                    adminPublicKey.Address,
                    ImmutableHashSet<Address>.Empty,
                    initialValidators: new Dictionary<PublicKey, BigInteger>
                    { { adminPrivateKey.PublicKey, BigInteger.One } });

            using var store = new DefaultStore(null);
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var actionEvaluator = new ActionEvaluator(
                new PolicyActionsRegistry(
                    beginBlockActionsGetter: _ => policy.BeginBlockActions,
                    endBlockActionsGetter: _ => policy.EndBlockActions,
                    beginTxActionsGetter: _ => policy.BeginTxActions,
                    endTxActionsGetter: _ => policy.EndTxActions),
                stateStore: stateStore,
                actionTypeLoader: new NCActionLoader()
            );
            var blockChain = BlockChain.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                actionEvaluator
            );

            int nonce = 0;
            List<Transaction> GenerateTransactions(int count)
            {
                var list = new List<Transaction>();
                for (int i = 0; i < count; i++)
                {
                    list.Add(Transaction.Create(
                        nonce++,
                        adminPrivateKey,
                        genesis.Hash,
                        Array.Empty<IValue>()
                    ));
                }

                return list;
            }

            Assert.Equal(1, blockChain.Count);
            var txs = GenerateTransactions(10).OrderBy(tx => tx.Id).ToList();
            PreEvaluationBlock preEvalBlock1 = new BlockContent(
                new BlockMetadata(
                    index: 1,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: null),
                transactions: txs).Propose();
            Block block1 = EvaluateAndSign(store, actionEvaluator, preEvalBlock1, adminPrivateKey);

            // Should be fine since policy hasn't kicked in yet.
            blockChain.Append(block1, GenerateBlockCommit(block1, adminPrivateKey));
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));

            txs = GenerateTransactions(10).OrderBy(tx => tx.Id).ToList();
            PreEvaluationBlock preEvalBlock2 = new BlockContent(
                new BlockMetadata(
                    index: 2,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: GenerateBlockCommit(blockChain.Tip, adminPrivateKey)),
                transactions: txs).Propose();
            Block block2 = EvaluateAndSign(store, actionEvaluator, preEvalBlock2, adminPrivateKey);

            // Subpolicy kicks in.
            Assert.Throws<InvalidBlockTxCountPerSignerException>(
                () => blockChain.Append(block2, GenerateBlockCommit(block2, adminPrivateKey)));
            Assert.Equal(2, blockChain.Count);
            Assert.False(blockChain.ContainsBlock(block2.Hash));
            // Since failed, roll back nonce.
            nonce -= 10;

            // Limit should also pass.
            txs = GenerateTransactions(5).OrderBy(tx => tx.Id).ToList();
            PreEvaluationBlock preEvalBlock3 = new BlockContent(
                new BlockMetadata(
                    index: 2,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: GenerateBlockCommit(blockChain.Tip, adminPrivateKey)),
                transactions: txs).Propose();
            Block block3 = EvaluateAndSign(store, actionEvaluator, preEvalBlock3, adminPrivateKey);
            blockChain.Append(block3, GenerateBlockCommit(block3, adminPrivateKey));
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block3.Hash));
        }

        private BlockCommit GenerateBlockCommit(Block block, PrivateKey key)
        {
            PrivateKey privateKey = key;
            return block.Index != 0
                ? new BlockCommit(
                    block.Index,
                    0,
                    block.Hash,
                    ImmutableArray<Vote>.Empty.Add(new VoteMetadata(
                        block.Index,
                        0,
                        block.Hash,
                        DateTimeOffset.UtcNow,
                        privateKey.PublicKey,
                        BigInteger.One,
                        VoteFlag.PreCommit).Sign(privateKey)))
                : null;
        }

        private Block MakeGenesisBlock(
            Address adminAddress,
            IImmutableSet<Address> activatedAddresses,
            AuthorizedMinersState authorizedMinersState = null,
            Dictionary<PublicKey, BigInteger> initialValidators = null,
            DateTimeOffset? timestamp = null,
            PendingActivationState[] pendingActivations = null,
            IEnumerable<ActionBase> actionBases = null,
            PrivateKey privateKey = null)
        {
            if (pendingActivations is null)
            {
                var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
                (ActivationKey activationKey, PendingActivationState pendingActivation) =
                    ActivationKey.Create(_privateKey, nonce);
                pendingActivations = new[] { pendingActivation };
            }

            var sheets = TableSheetsImporter.ImportSheets();
            return BlockHelper.ProposeGenesisBlock(
                sheets,
                new GoldDistribution[0],
                pendingActivations,
                new AdminState(adminAddress, 1500000),
                authorizedMinersState: authorizedMinersState,
                activatedAccounts: activatedAddresses,
                initialValidators: initialValidators,
                isActivateAdminAddress: false,
                credits: null,
                privateKey: privateKey ?? _privateKey,
                timestamp: timestamp ?? DateTimeOffset.MinValue,
                actionBases: actionBases);
        }

        private Block EvaluateAndSign(
            IStore store,
            ActionEvaluator actionEvaluator,
            PreEvaluationBlock preEvaluationBlock,
            PrivateKey privateKey
        )
        {
            if (preEvaluationBlock.Index < 1)
            {
                throw new ArgumentException(
                    $"Given {nameof(preEvaluationBlock)} must have block index " +
                    $"higher than 0");
            }

            if (preEvaluationBlock.ProtocolVersion < BlockMetadata.StateRootHashPostponeProtocolVersion)
            {
                throw new ArgumentException(
                    $"{nameof(preEvaluationBlock)} of which protocol version less than" +
                    $"{BlockMetadata.StateRootHashPostponeProtocolVersion} is not acceptable");
            }

            var stateRootHash = store.GetNextStateRootHash((BlockHash)preEvaluationBlock.PreviousHash);

            return preEvaluationBlock.Sign(privateKey, (HashDigest<SHA256>)stateRootHash);
        }
    }
}
