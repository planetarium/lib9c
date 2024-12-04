using System.Collections.Immutable;
using Lib9c.Renderers;
using Libplanet.Action;
using Libplanet.Blockchain.Policies;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Libplanet.Types.Tx;
using Nekoyume.Action;
using Nekoyume.Action.Loader;
using Nekoyume.Blockchain.Policy;
using Nekoyume.Model.State;
using System.Numerics;

namespace Lib9c.Proposer.Tests
{
    public class ProposerTest
    {
        private readonly PrivateKey _admin;
        private readonly PrivateKey _proposer;
        private readonly BlockChain _blockChain;

        public ProposerTest()
        {
            _admin = new PrivateKey();
            _proposer = new PrivateKey();
            var ncg = Currency.Uncapped("ncg", 2, null);
            var policy = new DebugPolicy();
            var actionTypeLoader = new NCActionLoader();
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            var mint = new PrepareRewardAssets
            {
                RewardPoolAddress = _proposer.Address,
                Assets = new List<FungibleAssetValue>
                {
                    1 * Currencies.Mead,
                },
            };

            var validatorSet = new ValidatorSet(
                new List<Validator> { new(_proposer.PublicKey, 10_000_000_000_000_000_000) });

            var initializeStates = new InitializeStates(
                validatorSet,
                new RankingState0(),
                new ShopState(),
                new Dictionary<string, string>(),
                new GameConfigState(),
                new RedeemCodeState(new Dictionary<PublicKey, RedeemCodeState.Reward>()),
                new ActivatedAccountsState(),
                new GoldCurrencyState(ncg),
                new GoldDistribution[] { },
                new PendingActivationState[] { });

            List <ActionBase> actions = new List<ActionBase>
            {
                initializeStates,
            };

            var genesis = BlockChain.ProposeGenesisBlock(
                privateKey: _proposer,
                transactions: ImmutableList<Transaction>.Empty
                    .Add(Transaction.Create(
                        0, _proposer, null, actions.ToPlainValues())),
                timestamp: DateTimeOffset.MinValue);

            var store = new MemoryStore();
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());

            _blockChain = BlockChain.Create(
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
        }

        [Fact]
        public void ProposeBlock()
        {
            Block block = _blockChain.ProposeBlock(_proposer);
            _blockChain.Append(
                block,
                GenerateBlockCommit(
                    block,
                    _proposer,
                    10_000_000_000_000_000_000));
        }

        [Fact]
        public void AssertInvalidProposer()
        {
            Block block = _blockChain.ProposeBlock(_proposer);
            Assert.Throws<InvalidBlockCommitException>(() => _blockChain.Append(
                block,
                GenerateBlockCommit(
                    block,
                    new PrivateKey(),
                    10_000_000_000_000_000_000)));
        }

        [Fact]
        public void AssertInvalidPower()
        {
            Block block = _blockChain.ProposeBlock(_proposer);
            Assert.Throws<InvalidBlockCommitException>(() => _blockChain.Append(
                block,
                GenerateBlockCommit(
                    block,
                    _proposer,
                    10_000_000_000_000_000)));
        }

        private BlockCommit? GenerateBlockCommit(
            Block block, PrivateKey privateKey, BigInteger power)
        {
            return block.Index != 0
                ? new BlockCommit(
                    block.Index,
                    0,
                    block.Hash,
                    ImmutableArray.Create(
                        new VoteMetadata(
                            block.Index,
                            0,
                            block.Hash,
                            DateTimeOffset.UtcNow,
                            privateKey.PublicKey,
                            power,
                            VoteFlag.PreCommit).Sign(privateKey)))
                : null;
        }
    }
}
