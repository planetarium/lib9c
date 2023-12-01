using System.Security.Cryptography;
using Lib9c.Plugin.Shared;
using Libplanet.Action;
using Libplanet.Common;
using Libplanet.Extensions.ActionEvaluatorCommonComponents;
using Libplanet.RocksDBStore;
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

        public byte[][] Evaluate(byte[] blockBytes, byte[]? baseStateRootHashBytes)
        {
            return _actionEvaluator.Evaluate(
                PreEvaluationBlockMarshaller.Deserialize(blockBytes),
                baseStateRootHashBytes is { } bytes ? new HashDigest<SHA256>(bytes) : null)
                .Select(eval => ActionEvaluationMarshaller.Serialize(eval)).ToArray();
        }
    }
}
