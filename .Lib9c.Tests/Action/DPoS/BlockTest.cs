namespace Lib9c.Tests.Action.DPoS
{
    using System;
    using System.Collections.Immutable;
    using Libplanet.Action;
    using Libplanet.Action.Loader;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Types.Tx;
    using Nekoyume.Action;
    using Nekoyume.Action.DPoS.Sys;
    using Serilog;
    using Xunit.Abstractions;

    public class BlockTest
    {
        private IBlockPolicy _policy;
        private BlockChain _blockChain;

        public BlockTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            var genesisProposer = new PrivateKey();
            _policy = new BlockPolicy(
                beginBlockActions: new IAction[] { new AllocateReward() }.ToImmutableArray(),
                endBlockActions: new IAction[]
                    {
                        new UpdateValidators(),
                        new RecordProposer(),
                    }.ToImmutableArray(),
                beginTxActions: new IAction[] { new Mortgage() }.ToImmutableArray(),
                endTxActions: new IAction[] { new Refund(), new Reward() }.ToImmutableArray());

            var store = new MemoryStore();
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var actionEvaluator = new ActionEvaluator(
                new PolicyActionsRegistry(
                    _ => _policy.BeginBlockActions,
                    _ => _policy.EndBlockActions,
                    _ => _policy.BeginTxActions,
                    _ => _policy.EndTxActions),
                stateStore: stateStore,
                actionTypeLoader: new SingleActionLoader(typeof(ActionBase)));
            var genesisBlock = BlockChain.ProposeGenesisBlock(
                    actionEvaluator,
                    transactions: ImmutableList<Transaction>.Empty,
                    privateKey: genesisProposer,
                    timestamp: DateTimeOffset.UtcNow);
            var blockChainStates = new BlockChainStates(store, stateStore);
            _blockChain = new BlockChain(
                policy: _policy,
                stagePolicy: new VolatileStagePolicy(),
                store: new MemoryStore(),
                stateStore: stateStore,
                genesisBlock: genesisBlock,
                blockChainStates: blockChainStates,
                actionEvaluator: actionEvaluator);
        }
    }
}
