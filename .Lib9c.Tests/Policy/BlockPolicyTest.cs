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
    using Libplanet.Types.Evidence;
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
        public void ValidateNextBlockTx_Mead()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.Address;
            var blockPolicySource = new BlockPolicySource();
            var actionTypeLoader = new NCActionLoader();
            var policy = blockPolicySource.GetPolicy(null, null, null, null);
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            var mint = new PrepareRewardAssets
            {
                RewardPoolAddress = adminAddress,
                Assets = new List<FungibleAssetValue>
                {
                    1 * Currencies.Mead,
                },
            };
            var mint2 = new PrepareRewardAssets
            {
                RewardPoolAddress = MeadConfig.PatronAddress,
                Assets = new List<FungibleAssetValue>
                {
                    1 * Currencies.Mead,
                },
            };
            var genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet<Address>.Empty,
                initialValidators: new Dictionary<PublicKey, BigInteger>
                    { { adminPrivateKey.PublicKey, BigInteger.One }, },
                actionBases: new[] { mint, mint2, },
                privateKey: adminPrivateKey
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
                    policy.PolicyActionsRegistry,
                    stateStore,
                    new NCActionLoader()
                ),
                new[] { new BlockRenderer(), }
            );

            var block = blockChain.ProposeBlock(adminPrivateKey);
            blockChain.Append(block, GenerateBlockCommit(block, adminPrivateKey));

            Assert.Equal(
                1 * Currencies.Mead,
                blockChain
                    .GetWorldState()
                    .GetBalance(adminAddress, Currencies.Mead));
            Assert.Equal(
                1 * Currencies.Mead,
                blockChain
                    .GetWorldState()
                    .GetBalance(MeadConfig.PatronAddress, Currencies.Mead));
            var action = new DailyReward
            {
                avatarAddress = adminAddress,
            };

            var txEmpty =
                Transaction.Create(
                    0,
                    adminPrivateKey,
                    genesis.Hash,
                    Array.Empty<IValue>()
                );
            Assert.IsType<TxPolicyViolationException>(BlockPolicySource.ValidateNextBlockTxRaw(blockChain, actionTypeLoader, txEmpty));

            var tx1 =
                Transaction.Create(
                    0,
                    adminPrivateKey,
                    genesis.Hash,
                    new ActionBase[] { action, }.ToPlainValues()
                );
            Assert.IsType<TxPolicyViolationException>(BlockPolicySource.ValidateNextBlockTxRaw(blockChain, actionTypeLoader, tx1));

            var tx2 =
                Transaction.Create(
                    1,
                    adminPrivateKey,
                    genesis.Hash,
                    gasLimit: 1,
                    maxGasPrice: new FungibleAssetValue(Currencies.Mead, 10, 10),
                    actions: new ActionBase[] { action, }.ToPlainValues()
                );
            Assert.Null(BlockPolicySource.ValidateNextBlockTxRaw(blockChain, actionTypeLoader, tx2));

            var tx3 =
                Transaction.Create(
                    2,
                    adminPrivateKey,
                    genesis.Hash,
                    gasLimit: 1,
                    maxGasPrice: new FungibleAssetValue(Currencies.Mead, 0, 0),
                    actions: new ActionBase[] { action, }.ToPlainValues()
                );
            Assert.Null(BlockPolicySource.ValidateNextBlockTxRaw(blockChain, actionTypeLoader, tx3));
        }

        [Fact]
        public void BlockCommitFromNonValidator()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.Address;
            var nonValidator = new PrivateKey();

            var blockPolicySource = new BlockPolicySource();
            var policy = blockPolicySource.GetPolicy(null, null, null, null);
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            var genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                initialValidators: new Dictionary<PublicKey, BigInteger>
                    { { adminPrivateKey.PublicKey, BigInteger.One }, }
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
                    policy.PolicyActionsRegistry,
                    stateStore,
                    new NCActionLoader()
                ),
                new[] { new BlockRenderer(), }
            );
            var block1 = blockChain.ProposeBlock(adminPrivateKey);
            Assert.Throws<InvalidBlockCommitException>(
                () => blockChain.Append(block1, GenerateBlockCommit(block1, nonValidator)));
        }

        [Fact]
        public void MustNotIncludeBlockActionAtTransaction()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.Address;
            var authorizedMinerPrivateKey = new PrivateKey();

            var (ak, ps) = ActivationKey.Create(
                new PrivateKey(),
                new byte[] { 0x00, 0x01, }
            );

            var blockPolicySource = new BlockPolicySource();
            var policy = blockPolicySource.GetPolicy(
                null,
                null,
                null,
                null);
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            var genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                new AuthorizedMinersState(
                    new[] { authorizedMinerPrivateKey.Address, },
                    5,
                    10
                ),
                pendingActivations: new[] { ps, }
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
                    policy.PolicyActionsRegistry,
                    stateStore,
                    actionLoader
                ),
                new[] { new BlockRenderer(), }
            );

            var unloadableAction = blockChain.MakeTransaction(
                adminPrivateKey,
                new ActionBase[] { new RewardGold(), }).Actions[0];
            Assert.Throws<InvalidActionException>(
                () =>
                    actionLoader.LoadAction(blockChain.Tip.Index, unloadableAction));
        }

        [Fact]
        public void EarnMiningGoldWhenSuccessMining()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.Address;
            var authorizedMinerPrivateKey = new PrivateKey();

            var (ak, ps) = ActivationKey.Create(
                new PrivateKey(),
                new byte[] { 0x00, 0x01, }
            );

            var blockPolicySource = new BlockPolicySource();
            var policy = blockPolicySource.GetPolicy(
                null,
                null,
                null,
                null);
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            var genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                new AuthorizedMinersState(
                    new[] { authorizedMinerPrivateKey.Address, },
                    5,
                    10
                ),
                new Dictionary<PublicKey, BigInteger> { { adminPrivateKey.PublicKey, BigInteger.One }, },
                pendingActivations: new[] { ps, }
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
                    policy.PolicyActionsRegistry,
                    stateStore,
                    new NCActionLoader()
                ),
                new[] { new BlockRenderer(), }
            );

            blockChain.MakeTransaction(
                adminPrivateKey,
                new ActionBase[] { new DailyReward(), }
            );

            var block = blockChain.ProposeBlock(adminPrivateKey);
            blockChain.Append(block, GenerateBlockCommit(block, adminPrivateKey));
            var actualBalance = blockChain
                .GetNextWorldState()
                .GetBalance(adminAddress, _currency);
            var expectedBalance = new FungibleAssetValue(_currency, 10, 0);
            Assert.True(expectedBalance.Equals(actualBalance));
        }

        [Fact]
        public void ValidateNextBlockWithManyTransactions()
        {
            var adminPrivateKey = new PrivateKey();
            var adminPublicKey = adminPrivateKey.PublicKey;
            var blockPolicySource = new BlockPolicySource();
            var policy = blockPolicySource.GetPolicy(
                null,
                null,
                MaxTransactionsPerBlockPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(0, null, null, 10)),
                null);
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            var genesis =
                MakeGenesisBlock(
                    adminPublicKey.Address,
                    ImmutableHashSet<Address>.Empty,
                    initialValidators: new Dictionary<PublicKey, BigInteger>
                        { { adminPrivateKey.PublicKey, BigInteger.One }, });

            using var store = new DefaultStore(null);
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var actionEvaluator = new ActionEvaluator(
                policy.PolicyActionsRegistry,
                stateStore,
                new NCActionLoader()
            );
            var blockChain = BlockChain.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                actionEvaluator
            );

            var nonce = 0;

            List<Transaction> GenerateTransactions(int count)
            {
                var list = new List<Transaction>();
                for (var i = 0; i < count; i++)
                {
                    list.Add(
                        Transaction.Create(
                            nonce++,
                            adminPrivateKey,
                            genesis.Hash,
                            Array.Empty<IValue>(),
                            gasLimit: 1,
                            maxGasPrice: new FungibleAssetValue(Currencies.Mead, 0, 0)
                        ));
                }

                return list;
            }

            Assert.Equal(1, blockChain.Count);
            var txs = GenerateTransactions(5).OrderBy(tx => tx.Id).ToList();
            var evs = new List<EvidenceBase>();
            var preEvalBlock1 = new BlockContent(
                new BlockMetadata(
                    1,
                    DateTimeOffset.MinValue,
                    adminPublicKey,
                    blockChain.Tip.Hash,
                    BlockContent.DeriveTxHash(txs),
                    null,
                    BlockContent.DeriveEvidenceHash(evs)),
                txs,
                evs).Propose();
            var stateRootHash = blockChain.DetermineNextBlockStateRootHash(blockChain.Tip, out _);
            var block1 = EvaluateAndSign(stateRootHash, preEvalBlock1, adminPrivateKey);
            blockChain.Append(block1, GenerateBlockCommit(block1, adminPrivateKey));
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));
            txs = GenerateTransactions(10).OrderBy(tx => tx.Id).ToList();
            var preEvalBlock2 = new BlockContent(
                new BlockMetadata(
                    2,
                    DateTimeOffset.MinValue,
                    adminPublicKey,
                    blockChain.Tip.Hash,
                    BlockContent.DeriveTxHash(txs),
                    GenerateBlockCommit(blockChain.Tip, adminPrivateKey),
                    BlockContent.DeriveEvidenceHash(evs)),
                txs,
                evs).Propose();
            stateRootHash = blockChain.DetermineNextBlockStateRootHash(blockChain.Tip, out _);
            var block2 = EvaluateAndSign(stateRootHash, preEvalBlock2, adminPrivateKey);
            blockChain.Append(block2, GenerateBlockCommit(block2, adminPrivateKey));
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block2.Hash));
            txs = GenerateTransactions(11).OrderBy(tx => tx.Id).ToList();
            var preEvalBlock3 = new BlockContent(
                new BlockMetadata(
                    3,
                    DateTimeOffset.MinValue,
                    adminPublicKey,
                    blockChain.Tip.Hash,
                    BlockContent.DeriveTxHash(txs),
                    GenerateBlockCommit(blockChain.Tip, adminPrivateKey),
                    BlockContent.DeriveEvidenceHash(evs)),
                txs,
                evs).Propose();
            stateRootHash = blockChain.DetermineNextBlockStateRootHash(blockChain.Tip, out _);
            var block3 = EvaluateAndSign(stateRootHash, preEvalBlock3, adminPrivateKey);
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
            var policy = blockPolicySource.GetPolicy(
                null,
                null,
                MaxTransactionsPerBlockPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(0, null, null, 10)),
                MaxTransactionsPerSignerPerBlockPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(2, null, null, 5)));
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            var genesis =
                MakeGenesisBlock(
                    adminPublicKey.Address,
                    ImmutableHashSet<Address>.Empty,
                    initialValidators: new Dictionary<PublicKey, BigInteger>
                        { { adminPrivateKey.PublicKey, BigInteger.One }, });

            using var store = new DefaultStore(null);
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var actionEvaluator = new ActionEvaluator(
                policy.PolicyActionsRegistry,
                stateStore,
                new NCActionLoader()
            );
            var blockChain = BlockChain.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                actionEvaluator
            );

            var nonce = 0;

            List<Transaction> GenerateTransactions(int count)
            {
                var list = new List<Transaction>();
                for (var i = 0; i < count; i++)
                {
                    list.Add(
                        Transaction.Create(
                            nonce++,
                            adminPrivateKey,
                            genesis.Hash,
                            Array.Empty<IValue>(),
                            gasLimit: 1,
                            maxGasPrice: new FungibleAssetValue(Currencies.Mead, 0, 0)
                        ));
                }

                return list;
            }

            Assert.Equal(1, blockChain.Count);
            var txs = GenerateTransactions(10).OrderBy(tx => tx.Id).ToList();
            var evs = new List<EvidenceBase>();
            var preEvalBlock1 = new BlockContent(
                new BlockMetadata(
                    1,
                    DateTimeOffset.MinValue,
                    adminPublicKey,
                    blockChain.Tip.Hash,
                    BlockContent.DeriveTxHash(txs),
                    null,
                    BlockContent.DeriveEvidenceHash(evs)),
                txs,
                evs).Propose();
            var stateRootHash = blockChain.DetermineNextBlockStateRootHash(blockChain.Tip, out _);
            var block1 = EvaluateAndSign(stateRootHash, preEvalBlock1, adminPrivateKey);

            // Should be fine since policy hasn't kicked in yet.
            blockChain.Append(block1, GenerateBlockCommit(block1, adminPrivateKey));
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));

            txs = GenerateTransactions(10).OrderBy(tx => tx.Id).ToList();
            var preEvalBlock2 = new BlockContent(
                new BlockMetadata(
                    2,
                    DateTimeOffset.MinValue,
                    adminPublicKey,
                    blockChain.Tip.Hash,
                    BlockContent.DeriveTxHash(txs),
                    GenerateBlockCommit(blockChain.Tip, adminPrivateKey),
                    BlockContent.DeriveEvidenceHash(evs)),
                txs,
                evs).Propose();
            stateRootHash = blockChain.DetermineNextBlockStateRootHash(blockChain.Tip, out _);
            var block2 = EvaluateAndSign(stateRootHash, preEvalBlock2, adminPrivateKey);

            // Subpolicy kicks in.
            Assert.Throws<InvalidBlockTxCountPerSignerException>(
                () => blockChain.Append(block2, GenerateBlockCommit(block2, adminPrivateKey)));
            Assert.Equal(2, blockChain.Count);
            Assert.False(blockChain.ContainsBlock(block2.Hash));
            // Since failed, roll back nonce.
            nonce -= 10;

            // Limit should also pass.
            txs = GenerateTransactions(5).OrderBy(tx => tx.Id).ToList();
            var preEvalBlock3 = new BlockContent(
                new BlockMetadata(
                    2,
                    DateTimeOffset.MinValue,
                    adminPublicKey,
                    blockChain.Tip.Hash,
                    BlockContent.DeriveTxHash(txs),
                    GenerateBlockCommit(blockChain.Tip, adminPrivateKey),
                    BlockContent.DeriveEvidenceHash(evs)),
                txs,
                evs).Propose();
            var block3 = EvaluateAndSign(stateRootHash, preEvalBlock3, adminPrivateKey);
            blockChain.Append(block3, GenerateBlockCommit(block3, adminPrivateKey));
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block3.Hash));
        }

        private BlockCommit GenerateBlockCommit(Block block, PrivateKey key)
        {
            var privateKey = key;
            return block.Index != 0
                ? new BlockCommit(
                    block.Index,
                    0,
                    block.Hash,
                    ImmutableArray<Vote>.Empty.Add(
                        new VoteMetadata(
                            block.Index,
                            0,
                            block.Hash,
                            DateTimeOffset.UtcNow,
                            privateKey.PublicKey,
                            null,
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
                var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03, };
                var (activationKey, pendingActivation) =
                    ActivationKey.Create(_privateKey, nonce);
                pendingActivations = new[] { pendingActivation, };
            }

            var sheets = TableSheetsImporter.ImportSheets();
            return BlockHelper.ProposeGenesisBlock(
                sheets,
                new GoldDistribution[0],
                pendingActivations,
                new AdminState(adminAddress, 1500000),
                authorizedMinersState,
                activatedAddresses,
                initialValidators,
                false,
                null,
                privateKey ?? _privateKey,
                timestamp ?? DateTimeOffset.MinValue,
                actionBases);
        }

        private Block EvaluateAndSign(
            HashDigest<SHA256> stateRootHash,
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

            if (preEvaluationBlock.ProtocolVersion < BlockMetadata.SlothProtocolVersion)
            {
                throw new ArgumentException(
                    $"{nameof(preEvaluationBlock)} of which protocol version less than" +
                    $"{BlockMetadata.SlothProtocolVersion} is not acceptable");
            }

            return preEvaluationBlock.Sign(privateKey, stateRootHash);
        }
    }
}
