using Bencodex.Types;

#nullable disable
namespace Nekoyume.Model.Item
{
    public interface ILock
    {
        LockType Type { get; }

        IValue Serialize();
    }
}
