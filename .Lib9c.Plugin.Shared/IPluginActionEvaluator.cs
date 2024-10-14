using Libplanet.Action;
using Libplanet.Action.Loader;

namespace Lib9c.Plugin.Shared
{
    public interface IPluginActionEvaluator
    {
        byte[][] Evaluate(byte[] blockBytes, byte[]? baseStateRootHashBytes);

        IActionLoader ActionLoader { get; }

        IPolicyActionsRegistry PolicyActionsRegistry { get; }
    }
}
