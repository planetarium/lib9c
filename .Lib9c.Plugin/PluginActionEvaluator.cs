using System.Collections.Immutable;
using System.Security.Cryptography;
using Lib9c.Action;
using Lib9c.Action.Loader;
using Lib9c.Action.ValidatorDelegation;
using Lib9c.Plugin.Shared;
using Libplanet.Action;
using Libplanet.Common;
using Lib9c.ActionEvaluatorCommonComponents;
using Libplanet.Store;

namespace Lib9c.Plugin
{
    public class PluginActionEvaluator : IPluginActionEvaluator
    {
        private readonly IActionEvaluator _actionEvaluator;

        public PluginActionEvaluator(IPluginKeyValueStore keyValueStore)
        {
            var stateStore = new TrieStateStore(new WrappedKeyValueStore(keyValueStore));
            _actionEvaluator = new ActionEvaluator(
                new PolicyActionsRegistry(
                    beginBlockActions: new IAction[] {
                        new SlashValidator(),
                        new AllocateGuildReward(),
                        new AllocateReward(),
                    }.ToImmutableArray(),
                    endBlockActions: new IAction[] {
                        new UpdateValidators(),
                        new RecordProposer(),
                        new RewardGold(),
                    }.ToImmutableArray(),
                    beginTxActions: new IAction[] {
                        new Mortgage(),
                    }.ToImmutableArray(),
                    endTxActions: new IAction[] {
                        new Reward(), new Refund(),
                    }.ToImmutableArray()),
                stateStore,
                new NCActionLoader());
        }

        public byte[][] Evaluate(byte[] blockBytes, byte[]? baseStateRootHashBytes)
        {
            var evals = _actionEvaluator.Evaluate(
                PreEvaluationBlockMarshaller.Deserialize(blockBytes),
                baseStateRootHashBytes is { } bytes ? new HashDigest<SHA256>(bytes) : null);
            return evals.Select(eval => ActionEvaluationMarshaller.Serialize(eval)).ToArray();
        }
    }
}
