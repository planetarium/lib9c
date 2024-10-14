using System.Collections.Immutable;
using System.Security.Cryptography;
using Lib9c.Plugin.Shared;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Common;
using Libplanet.Extensions.ActionEvaluatorCommonComponents;
using Libplanet.Store;
using Nekoyume.Action;
using Nekoyume.Action.Loader;


namespace Lib9c.Plugin
{
    public class PluginActionEvaluator : IPluginActionEvaluator
    {
        private readonly IActionEvaluator _actionEvaluator;

        public PluginActionEvaluator(IPluginKeyValueStore keyValueStore)
        {
            ActionLoader = new NCActionLoader();
            PolicyActionsRegistry = new PolicyActionsRegistry(
                beginBlockActions: ImmutableArray<IAction>.Empty,
                endBlockActions: new IAction[]
                {
                    new RewardGold()
                }.ToImmutableArray(),
                beginTxActions: ImmutableArray<IAction>.Empty,
                endTxActions: ImmutableArray<IAction>.Empty);

            var stateStore = new TrieStateStore(new WrappedKeyValueStore(keyValueStore));

            _actionEvaluator = new ActionEvaluator(
                PolicyActionsRegistry,
                stateStore,
                ActionLoader);
        }

        public byte[][] Evaluate(byte[] blockBytes, byte[]? baseStateRootHashBytes)
        {
            var evals = _actionEvaluator.Evaluate(
                PreEvaluationBlockMarshaller.Deserialize(blockBytes),
                baseStateRootHashBytes is { } bytes ? new HashDigest<SHA256>(bytes) : null);
            return evals.Select(eval => eval.Serialize()).ToArray();
        }
        public IActionLoader ActionLoader { get; }

        public IPolicyActionsRegistry PolicyActionsRegistry { get; }
    }
}
