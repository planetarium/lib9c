using System.Security.Cryptography;
using Lib9c.Plugin.Shared;
using Libplanet.Action;
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
            var stateStore = new TrieStateStore(new WrappedKeyValueStore(keyValueStore));
            _actionEvaluator = new ActionEvaluator(
                _ => new RewardGold(),
                stateStore,
                new NCActionLoader());
        }

        public byte[][] Evaluate(byte[] blockBytes, byte[]? baseStateRootHashBytes, out byte[] stateRootHashBytes)
        {
            var evals = _actionEvaluator.Evaluate(
                PreEvaluationBlockMarshaller.Deserialize(blockBytes),
                baseStateRootHashBytes is { } bytes ? new HashDigest<SHA256>(bytes) : null,
                out var stateRootHash);
            stateRootHashBytes = stateRootHash.ToByteArray();
            return evals.Select(eval => ActionEvaluationMarshaller.Serialize(eval)).ToArray();
        }
    }
}
