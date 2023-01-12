using Libplanet;

namespace Lib9c.Action
{
    internal interface IActivateAction
    {
        // TODO: We should convert them to property after updating C# version...
        Address GetPendingAddress();

        byte[] GetSignature();
    }
}
