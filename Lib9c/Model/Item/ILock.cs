using Bencodex.Types;

namespace Lib9c.Model.Item
{
    public interface ILock
    {
        LockType Type { get; }

        IValue Serialize();
    }
}
