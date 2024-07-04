namespace Lib9c.Tests.Action.DPoS
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Renderers;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Tx;
    using Nekoyume.Action;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Sys;
    using Nekoyume.Action.Loader;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class MeadTest
    {
        private static readonly byte[] ConversionTable =
        {
            48,  // '0'
            49,  // '1'
            50,  // '2'
            51,  // '3'
            52,  // '4'
            53,  // '5'
            54,  // '6'
            55,  // '7'
            56,  // '8'
            57,  // '9'
            97,  // 'a'
            98,  // 'b'
            99,  // 'c'
            100, // 'd'
            101, // 'e'
            102, // 'f'
        };

        private PrivateKey _genesisProposer;
        private IStore _store;
        private ActionEvaluator _actionEvaluator;
        private BlockChain _chain;

        public MeadTest()
        {
            _genesisProposer = new PrivateKey();
            _store = new MemoryStore();
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var trie = stateStore.GetStateRoot(null);
            trie = trie.SetMetadata(new TrieMetadata(BlockMetadata.CurrentProtocolVersion));
            IWorld world = new World(new WorldBaseState(trie, stateStore));
            world = world.MintAsset(
                new ActionContext { Signer = _genesisProposer.Address },
                _genesisProposer.Address,
                Currencies.Mead * 1000);
            world = world.SetLegacyState(
                GoldCurrencyState.Address,
                new GoldCurrencyState(Currency.Legacy("NCG", 2, null)).Serialize());

            var worldTrie = world.Trie;
            foreach (var account in world.Delta.Accounts)
            {
                var accountTrie = stateStore.Commit(account.Value.Trie);
                worldTrie = worldTrie.Set(
                    ToStateKey(account.Key),
                    new Binary(accountTrie.Hash.ByteArray));
            }

            worldTrie = stateStore.Commit(worldTrie);
            // Create a policy without RewardGold action.
            var policy = new BlockPolicy(
                new IAction[] { new AllocateReward() }.ToImmutableArray(),
                new IAction[] { new RecordProposer() }.ToImmutableArray(),
                new IAction[] { new Mortgage() }.ToImmutableArray(),
                new IAction[] { new Reward(), new Refund() }.ToImmutableArray());
            var preEval = new BlockContent(
                new BlockMetadata(
                    protocolVersion: BlockMetadata.CurrentProtocolVersion,
                    index: 0,
                    timestamp: DateTimeOffset.UtcNow,
                    miner: _genesisProposer.Address,
                    publicKey: _genesisProposer.PublicKey,
                    previousHash: null,
                    txHash: null,
                    lastCommit: null),
                transactions: Enumerable.Empty<Transaction>()).Propose();
            var genesisBlock = preEval.Sign(_genesisProposer, worldTrie.Hash);
            var blockChainStates = new BlockChainStates(_store, stateStore);
            _actionEvaluator = new ActionEvaluator(
                new PolicyActionsRegistry(
                    _ => policy.BeginBlockActions,
                    _ => policy.EndBlockActions,
                    _ => policy.BeginTxActions,
                    _ => policy.EndTxActions),
                stateStore: stateStore,
                actionTypeLoader: new NCActionLoader());
            _chain = BlockChain.Create(
                policy,
                new VolatileStagePolicy(),
                _store,
                stateStore,
                genesisBlock,
                renderers: new[] { new ActionRenderer() },
                blockChainStates: blockChainStates,
                actionEvaluator: _actionEvaluator
            );
        }

        [Fact]
        public void AllocateMeadReward()
        {
            var tx = Transaction.Create(
                nonce: 0,
                privateKey: _genesisProposer,
                genesisHash: _chain.Genesis.Hash,
                maxGasPrice: Currencies.Mead * 1,
                gasLimit: 2,
                actions: new[]
                {
                    new CreateAvatar
                    {
                        name = "Foo",
                        index = 0,
                        ear = 0,
                        hair = 0,
                        lens = 0,
                        tail = 0,
                    },
                }.ToPlainValues());

            _chain.StageTransaction(tx);
            Block block = _chain.ProposeBlock(_genesisProposer);
            Assert.Single(block.Transactions);

            var evaluations = _actionEvaluator.Evaluate(
                block, _store.GetNextStateRootHash((BlockHash)block.PreviousHash));

            // 5 policy actions + 1 CreateAvatar action
            Assert.Equal(6, evaluations.Count);
            Assert.Equal(
                Currencies.Mead * 999,
                _chain
                    .GetWorldState(evaluations.Last().OutputState)
                    .GetBalance(_genesisProposer.Address, Currencies.Mead));
            Assert.Equal(
                Currencies.Mead * 1,
                _chain
                    .GetWorldState(evaluations.Last().OutputState)
                    .GetBalance(ReservedAddress.RewardPool, Currencies.Mead));
        }

        private static KeyBytes ToStateKey(Address address)
        {
            var addressBytes = address.ByteArray;
            byte[] buffer = new byte[addressBytes.Length * 2];
            for (int i = 0; i < addressBytes.Length; i++)
            {
                buffer[i * 2] = ConversionTable[addressBytes[i] >> 4];
                buffer[i * 2 + 1] = ConversionTable[addressBytes[i] & 0xf];
            }

            return new KeyBytes(buffer);
        }
    }
}
