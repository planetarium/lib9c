namespace Lib9c.Plugin.Shared
{
    public interface IPluginActionEvaluator
    {
        byte[][] Evaluate(byte[] blockBytes, byte[]? baseStateRootHashBytes);
    }
}
