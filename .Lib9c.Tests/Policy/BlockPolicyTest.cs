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
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Libplanet.Action.Loader;
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
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Loader;
    using Nekoyume.Action.ValidatorDelegation;
    using Nekoyume.Blockchain.Policy;
    using Nekoyume.Model;
    using Nekoyume.Model.Guild;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class BlockPolicyTest
    {
        private readonly PrivateKey _privateKey;

        public BlockPolicyTest()
        {
            _privateKey = new PrivateKey();
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
            Block genesis = MakeGenesisBlock(
                new ValidatorSet(
                    new List<Validator> { new (adminPrivateKey.PublicKey, 10_000_000_000_000_000_000) }),
                adminAddress,
                ImmutableHashSet<Address>.Empty,
                actionBases: new[] { mint, mint2 },
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

            Block block = blockChain.ProposeBlock(adminPrivateKey);
            blockChain.Append(
                block,
                GenerateBlockCommit(
                    block,
                    blockChain.GetNextWorldState().GetValidatorSet(),
                    new PrivateKey[] { adminPrivateKey }));

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
            Block genesis = MakeGenesisBlock(
                new ValidatorSet(
                    new List<Validator> { new (adminPrivateKey.PublicKey, 10_000_000_000_000_000_000) }),
                adminAddress,
                ImmutableHashSet.Create(adminAddress)
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
            Block block1 = blockChain.ProposeBlock(adminPrivateKey);
            var invalidBlockCommit = new BlockCommit(
                block1.Index,
                0,
                block1.Hash,
                new[]
                {
                    new VoteMetadata(
                        block1.Index,
                        0,
                        block1.Hash,
                        DateTimeOffset.UtcNow,
                        nonValidator.PublicKey,
                        1000,
                        VoteFlag.PreCommit
                    ).Sign(nonValidator),
                }.ToImmutableArray());
            Assert.Throws<InvalidBlockCommitException>(
                () => blockChain.Append(
                    block1,
                    invalidBlockCommit));
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
            Block genesis = MakeGenesisBlock(
                new ValidatorSet(
                    new List<Validator> { new (adminPrivateKey.PublicKey, 10_000_000_000_000_000_000) }),
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
        public void EarnMiningMeadWhenSuccessMining()
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
            Block genesis = MakeGenesisBlock(
                new ValidatorSet(
                    new List<Validator> { new (adminPrivateKey.PublicKey, 10_000_000_000_000_000_000) }),
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                new AuthorizedMinersState(
                    new[] { authorizedMinerPrivateKey.Address, },
                    5,
                    10
                ),
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

            Block block = blockChain.ProposeBlock(adminPrivateKey);
            BigInteger power = blockChain.GetNextWorldState().GetValidatorSet().GetValidator(adminPrivateKey.PublicKey).Power;
            BlockCommit commit = GenerateBlockCommit(
                block,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { adminPrivateKey });
            // Since it's a block right after the Genesis, the reward is 0.
            blockChain.Append(block, commit);

            var mintAmount = 10 * Currencies.Mead;
            var mint = new PrepareRewardAssets
            {
                RewardPoolAddress = adminAddress,
                Assets = new List<FungibleAssetValue>
                {
                    mintAmount,
                },
            };

            blockChain.MakeTransaction(
                adminPrivateKey,
                new ActionBase[] { mint, }
            );
            block = blockChain.ProposeBlock(adminPrivateKey, commit);
            power = blockChain.GetNextWorldState().GetValidatorSet().GetValidator(adminPrivateKey.PublicKey).Power;
            commit = GenerateBlockCommit(
                block,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { adminPrivateKey });
            // First Reward : Proposer base reward 5 * 0.01, proposer bonus reward 5 * 0.04, Commission 4.75 * 0.1
            // Total 10 + 0.05 + 0.2 + 0.475 = 10.725
            blockChain.Append(block, commit);

            var rewardCurrency = Currencies.Mead;
            var actualBalance = blockChain
                .GetNextWorldState()
                .GetBalance(adminAddress, rewardCurrency);
            var expectedBalance = mintAmount + new FungibleAssetValue(rewardCurrency, 0, 725000000000000000);
            Assert.Equal(expectedBalance, actualBalance);

            var ssss = blockChain
                .GetNextWorldState()
                .GetBalance(Addresses.RewardPool, rewardCurrency);

            // After claimed, mead have to be used?
            blockChain.MakeTransaction(
                adminPrivateKey,
                new ActionBase[] { new ClaimValidatorRewardSelf(), },
                gasLimit: 1,
                maxGasPrice: Currencies.Mead * 1
            );

            block = blockChain.ProposeBlock(adminPrivateKey, commit);
            power = blockChain.GetNextWorldState().GetValidatorSet().GetValidator(adminPrivateKey.PublicKey).Power;
            commit = GenerateBlockCommit(
                block,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { adminPrivateKey });
            // First + Second Reward : Total reward of two blocks : 10 * 2 = 20
            // Base reward: 0.05 + 0.2 + 0.475 = 0.725
            // Total reward: 4.275 + 4.275 (two blocks)
            // Used gas: 1
            // Total 10.725 + 0.725 + 4.275 + 4.275 - 1 = 19
            blockChain.Append(block, commit);

            actualBalance = blockChain
                .GetNextWorldState()
                .GetBalance(adminAddress, rewardCurrency);
            expectedBalance = rewardCurrency * 19;
            Assert.Equal(expectedBalance, actualBalance);

            block = blockChain.ProposeBlock(adminPrivateKey, commit);
            power = blockChain.GetNextWorldState().GetValidatorSet().GetValidator(adminPrivateKey.PublicKey).Power;
            commit = GenerateBlockCommit(
                block,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { adminPrivateKey });
            // Mining reward: 5 + 1 / 2 = 5.5
            // Proposer base reward 5.5 * 0.01, proposer bonus reward 5.5 * 0.04, Commission (5.5 - 0.275) * 0.1
            // Base reward: (5.5 * 0.01 + 5.5 * 0.04) + (5.5 - 0.275) * 0.1 = 0.7975
            // Total 19 + 0.7975 = 19.7975
            blockChain.Append(block, commit);
            actualBalance = blockChain
                .GetNextWorldState()
                .GetBalance(adminAddress, rewardCurrency);
            expectedBalance = new FungibleAssetValue(rewardCurrency, 19, 797500000000000000);
            Assert.Equal(expectedBalance, actualBalance);
        }

        [Theory]
        [InlineData(10L, 1L, 50, 5L)]
        [InlineData(1L, 10L, 50, 5L)]
        [InlineData(1L, 1L, 50, 5L)]
        [InlineData(10L, 1L, 3, 5L)]
        public void PayMeadWithGuild(long guildMint, long signerMint, int contract, long consumption)
        {
            var validatorPrivateKey = new PrivateKey();
            var adminPrivateKey = new PrivateKey();
            var guildMasterPrivateKey = new PrivateKey();
            var signerPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.Address;
            var authorizedMinerPrivateKey = new PrivateKey();

            var (ak, ps) = ActivationKey.Create(
                new PrivateKey(),
                new byte[] { 0x00, 0x01, }
            );

            var blockPolicySource = new BlockPolicySource(actionLoader: new GasActionLoader());
            var policy = blockPolicySource.GetPolicy(
                null,
                null,
                null,
                null);
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            Block genesis = MakeGenesisBlock(
                new ValidatorSet(
                    new List<Validator> { new (validatorPrivateKey.PublicKey, 10_000_000_000_000_000_000) }),
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                new AuthorizedMinersState(
                    new[] { authorizedMinerPrivateKey.Address, },
                    5,
                    10
                ),
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
                    policy.PolicyActionsRegistry,
                    stateStore,
                    new GasActionLoader()
                ),
                new[] { new BlockRenderer(), }
            );

            var adminMintAmount = Currencies.Mead * 10;
            var mintToAdmin = new PrepareRewardAssets
            {
                RewardPoolAddress = adminAddress,
                Assets = new List<FungibleAssetValue>
                {
                    adminMintAmount,
                },
            };

            var signerMintAmount = Currencies.Mead * signerMint;
            var mintToSigner = new PrepareRewardAssets
            {
                RewardPoolAddress = signerPrivateKey.Address,
                Assets = new List<FungibleAssetValue>
                {
                    signerMintAmount,
                },
            };

            blockChain.MakeTransaction(
                adminPrivateKey,
                new ActionBase[] { mintToAdmin, }
            );

            blockChain.MakeTransaction(
                adminPrivateKey,
                new ActionBase[] { mintToSigner, }
            );

            var block = blockChain.ProposeBlock(adminPrivateKey);
            var power = blockChain.GetNextWorldState().GetValidatorSet().GetValidator(validatorPrivateKey.PublicKey).Power;
            var commit = GenerateBlockCommit(
                block,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { validatorPrivateKey });
            blockChain.Append(block, commit);

            blockChain.MakeTransaction(
                guildMasterPrivateKey,
                new ActionBase[] { new MakeGuild(validatorPrivateKey.Address), }
            );

            var requestPledge = new RequestPledgePatron
            {
                AgentAddress = signerPrivateKey.Address,
                RefillMead = contract,
            };

            blockChain.MakeTransaction(
                adminPrivateKey,
                new ActionBase[] { requestPledge }
            );

            block = blockChain.ProposeBlock(adminPrivateKey, commit);
            power = blockChain.GetNextWorldState().GetValidatorSet().GetValidator(validatorPrivateKey.PublicKey).Power;
            commit = GenerateBlockCommit(
                block,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { validatorPrivateKey });
            blockChain.Append(block, commit);

            var repo = new GuildRepository(new World(blockChain.GetNextWorldState()), new ActionContext { });
            var guildAddress = repo.GetJoinedGuild(new AgentAddress(guildMasterPrivateKey.Address)).Value;

            var guildMintAmount = Currencies.Mead * guildMint;
            var mintToGuild = new PrepareRewardAssets
            {
                RewardPoolAddress = (Address)guildAddress,
                Assets = new List<FungibleAssetValue>
                {
                    guildMintAmount,
                },
            };

            var approvePledge = new ApprovePledge
            {
                PatronAddress = MeadConfig.PatronAddress,
            };

            var joinGuild = new JoinGuild(guildAddress);

            blockChain.MakeTransaction(
                adminPrivateKey,
                new ActionBase[] { mintToGuild, }
            );

            blockChain.MakeTransaction(
                signerPrivateKey,
                new ActionBase[] { approvePledge, }
            );

            blockChain.MakeTransaction(
                signerPrivateKey,
                new ActionBase[] { joinGuild, }
            );

            block = blockChain.ProposeBlock(adminPrivateKey, commit);
            power = blockChain.GetNextWorldState().GetValidatorSet().GetValidator(validatorPrivateKey.PublicKey).Power;
            commit = GenerateBlockCommit(
                block,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { validatorPrivateKey });
            blockChain.Append(block, commit);
            var actualBalance = blockChain
                .GetNextWorldState()
                .GetBalance(signerPrivateKey.Address, Currencies.Mead);
            var actualGuildBalance = blockChain
                .GetNextWorldState()
                .GetBalance(guildAddress, Currencies.Mead);
            var expectedBalance = signerMintAmount;
            var expectedGuildBalance = guildMintAmount;
            Assert.Equal(expectedBalance, actualBalance);
            Assert.Equal(expectedGuildBalance, actualGuildBalance);

            var gasAction = new GasAction { Consumption = consumption };
            var gasTx = blockChain.MakeTransaction(
                signerPrivateKey,
                new ActionBase[] { gasAction, },
                maxGasPrice: Currencies.Mead * 1,
                gasLimit: 10L
            );

            block = blockChain.ProposeBlock(adminPrivateKey, commit);
            power = blockChain.GetNextWorldState().GetValidatorSet().GetValidator(validatorPrivateKey.PublicKey).Power;
            commit = GenerateBlockCommit(
                block,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { validatorPrivateKey });
            blockChain.Append(block, commit);

            actualBalance = blockChain
                .GetNextWorldState()
                .GetBalance(signerPrivateKey.Address, Currencies.Mead);
            actualGuildBalance = blockChain
                .GetNextWorldState()
                .GetBalance(guildAddress, Currencies.Mead);

            var failed = false;
            if (contract > consumption && consumption < guildMint)
            {
                expectedGuildBalance = guildMintAmount - Currencies.Mead * consumption;
            }
            else
            {
                if (consumption < signerMint)
                {
                    expectedBalance = signerMintAmount - Currencies.Mead * consumption;
                }
                else
                {
                    expectedBalance = Currencies.Mead * 0;
                    failed = true;
                }
            }

            Assert.Equal(failed, blockChain.GetTxExecution(block.Hash, gasTx.Id).Fail);
            Assert.Equal(expectedBalance, actualBalance);
            Assert.Equal(expectedGuildBalance, actualGuildBalance);
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
                    validators: new ValidatorSet(
                        new List<Validator> { new (adminPublicKey, 10_000_000_000_000_000_000) }),
                    adminPublicKey.Address,
                    ImmutableHashSet<Address>.Empty);

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
            Block block1 = EvaluateAndSign(stateRootHash, preEvalBlock1, adminPrivateKey);
            blockChain.Append(block1, GenerateBlockCommit(
                block1,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { adminPrivateKey }));
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));
            txs = GenerateTransactions(10).OrderBy(tx => tx.Id).ToList();
            var blockCommit = GenerateBlockCommit(
                blockChain.Tip,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { adminPrivateKey });
            PreEvaluationBlock preEvalBlock2 = new BlockContent(
                new BlockMetadata(
                    index: 2,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: blockCommit,
                    evidenceHash: BlockContent.DeriveEvidenceHash(evs)),
                transactions: txs,
                evidence: evs).Propose();
            stateRootHash = blockChain.DetermineNextBlockStateRootHash(blockChain.Tip, out _);
            Block block2 = EvaluateAndSign(stateRootHash, preEvalBlock2, adminPrivateKey);
            blockChain.Append(
                block2,
                GenerateBlockCommit(
                    block2,
                    blockChain.GetNextWorldState().GetValidatorSet(),
                    new PrivateKey[] { adminPrivateKey }));
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block2.Hash));
            txs = GenerateTransactions(11).OrderBy(tx => tx.Id).ToList();
            blockCommit = GenerateBlockCommit(
                blockChain.Tip,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { adminPrivateKey });
            PreEvaluationBlock preEvalBlock3 = new BlockContent(
                new BlockMetadata(
                    index: 3,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: blockCommit,
                    evidenceHash: BlockContent.DeriveEvidenceHash(evs)),
                transactions: txs,
                evidence: evs).Propose();
            stateRootHash = blockChain.DetermineNextBlockStateRootHash(blockChain.Tip, out _);
            var block3 = EvaluateAndSign(stateRootHash, preEvalBlock3, adminPrivateKey);
            Assert.Throws<InvalidBlockTxCountException>(
                () => blockChain.Append(
                    block3,
                    GenerateBlockCommit(
                        block3,
                        blockChain.GetNextWorldState().GetValidatorSet(),
                        new PrivateKey[] { adminPrivateKey })));
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
            var validatorSet = new ValidatorSet(
                new List<Validator> { new (adminPrivateKey.PublicKey, 10_000_000_000_000_000_000) });
            Block genesis =
                MakeGenesisBlock(
                    validatorSet,
                    adminPublicKey.Address,
                    ImmutableHashSet<Address>.Empty);

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
            blockChain.Append(
                block1,
                GenerateBlockCommit(
                    block1,
                    blockChain.GetNextWorldState().GetValidatorSet(),
                    new PrivateKey[] { adminPrivateKey }));
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));

            txs = GenerateTransactions(10).OrderBy(tx => tx.Id).ToList();
            var blockCommit = GenerateBlockCommit(
                blockChain.Tip,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { adminPrivateKey });
            PreEvaluationBlock preEvalBlock2 = new BlockContent(
                new BlockMetadata(
                    index: 2,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: blockCommit,
                    evidenceHash: BlockContent.DeriveEvidenceHash(evs)),
                transactions: txs,
                evidence: evs).Propose();
            stateRootHash = blockChain.DetermineNextBlockStateRootHash(blockChain.Tip, out _);
            var block2 = EvaluateAndSign(stateRootHash, preEvalBlock2, adminPrivateKey);

            // Subpolicy kicks in.
            Assert.Throws<InvalidBlockTxCountPerSignerException>(
                () => blockChain.Append(
                    block2,
                    GenerateBlockCommit(
                        block2,
                        blockChain.GetNextWorldState().GetValidatorSet(),
                        new PrivateKey[] { adminPrivateKey })));
            Assert.Equal(2, blockChain.Count);
            Assert.False(blockChain.ContainsBlock(block2.Hash));
            // Since failed, roll back nonce.
            nonce -= 10;

            // Limit should also pass.
            txs = GenerateTransactions(5).OrderBy(tx => tx.Id).ToList();
            blockCommit = GenerateBlockCommit(
                blockChain.Tip,
                blockChain.GetNextWorldState().GetValidatorSet(),
                new PrivateKey[] { adminPrivateKey });
            PreEvaluationBlock preEvalBlock3 = new BlockContent(
                new BlockMetadata(
                    index: 2,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: blockCommit,
                    evidenceHash: BlockContent.DeriveEvidenceHash(evs)),
                transactions: txs,
                evidence: evs).Propose();
            Block block3 = EvaluateAndSign(stateRootHash, preEvalBlock3, adminPrivateKey);
            blockChain.Append(
                block3,
                GenerateBlockCommit(
                    block3,
                    blockChain.GetNextWorldState().GetValidatorSet(),
                    new PrivateKey[] { adminPrivateKey }));
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block3.Hash));
        }

        private BlockCommit GenerateBlockCommit(
            Block block, ValidatorSet validatorSet, IEnumerable<PrivateKey> validatorPrivateKeys)
        {
            return block.Index != 0
                ? new BlockCommit(
                    block.Index,
                    0,
                    block.Hash,
                    validatorPrivateKeys.Select(k => new VoteMetadata(
                        block.Index,
                        0,
                        block.Hash,
                        DateTimeOffset.UtcNow,
                        k.PublicKey,
                        validatorSet.GetValidator(k.PublicKey).Power,
                        VoteFlag.PreCommit).Sign(k)).ToImmutableArray())
                : null;
        }

        private Block MakeGenesisBlock(
            ValidatorSet validators,
            Address adminAddress,
            IImmutableSet<Address> activatedAddresses,
            AuthorizedMinersState authorizedMinersState = null,
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
                validators,
                sheets,
                new GoldDistribution[0],
                pendingActivations,
                new AdminState(adminAddress, 1500000),
                authorizedMinersState: authorizedMinersState,
                activatedAccounts: activatedAddresses,
                isActivateAdminAddress: false,
                credits: null,
                minerPrivateKey: privateKey ?? _privateKey,
                signerPrivateKey: privateKey ?? _privateKey,
                timestamp: timestamp ?? DateTimeOffset.MinValue,
                actionBases: actionBases);
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

        [ActionType(TypeIdentifier)]
        protected class GasAction : ActionBase
        {
            public const string TypeIdentifier = "gas_action";

            public GasAction()
            {
            }

            public long Consumption { get; set; }

            public override IValue PlainValue => Dictionary.Empty
               .Add("type_id", TypeIdentifier)
               .Add("consumption", new Integer(Consumption));

            public override void LoadPlainValue(IValue plainValue)
            {
                if (plainValue is not Dictionary root ||
                    !root.TryGetValue((Text)"consumption", out var rawValues) ||
                    rawValues is not Integer value)
                {
                    throw new InvalidCastException();
                }

                Consumption = (long)value.Value;
            }

            public override IWorld Execute(IActionContext context)
            {
                GasTracer.UseGas(Consumption);
                return context.PreviousState;
            }
        }

        [ActionType(TypeIdentifier)]
        protected class RequestPledgePatron : ActionBase
        {
            public const string TypeIdentifier = "request_pledge_patron";

            public RequestPledgePatron()
            {
            }

            public Address AgentAddress { get; set; }

            public int RefillMead { get; set; }

            public override IValue PlainValue =>
                Dictionary.Empty
                    .Add("type_id", TypeIdentifier)
                    .Add("values", List.Empty.Add(AgentAddress.Serialize()).Add(RefillMead.Serialize()));

            public override void LoadPlainValue(IValue plainValue)
            {
                List values = (List)((Dictionary)plainValue)["values"];
                AgentAddress = values[0].ToAddress();
                RefillMead = values[1].ToInteger();
            }

            public override IWorld Execute(IActionContext context)
            {
                GasTracer.UseGas(1);
                var states = context.PreviousState;
                var contractAddress = AgentAddress.GetPledgeAddress();
                if (states.TryGetLegacyState(contractAddress, out List _))
                {
                    throw new AlreadyContractedException($"{AgentAddress} already contracted.");
                }

                return states
                    .SetLegacyState(
                        contractAddress,
                        List.Empty
                            .Add(MeadConfig.PatronAddress.Serialize())
                            .Add(false.Serialize())
                            .Add(RefillMead.Serialize())
                    );
            }
        }

        protected class GasActionLoader : IActionLoader
        {
            private readonly NCActionLoader _actionLoader;

            public GasActionLoader()
            {
                _actionLoader = new NCActionLoader();
            }

            public IAction LoadAction(long index, IValue value)
            {
                if (value is Dictionary pv &&
                    pv.TryGetValue((Text)"type_id", out IValue rawTypeId) &&
                    rawTypeId is Text typeId && typeId == GasAction.TypeIdentifier)
                {
                    var action = new GasAction();
                    action.LoadPlainValue(pv);
                    return action;
                }

                if (value is Dictionary pv2 &&
                    pv2.TryGetValue((Text)"type_id", out IValue rawTypeId2) &&
                    rawTypeId2 is Text typeId2 && typeId2 == RequestPledgePatron.TypeIdentifier)
                {
                    var action = new RequestPledgePatron();
                    action.LoadPlainValue(pv2);
                    return action;
                }

                return _actionLoader.LoadAction(index, value);
            }
        }
    }
}
