using Bencodex.Types;

namespace Lib9c.Model.State
{
    public interface IState
    {
        IValue Serialize();
    }
}
