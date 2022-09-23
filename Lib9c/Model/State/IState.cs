using Bencodex.Types;

#nullable disable
namespace Nekoyume.Model.State
{
    public interface IState
    {
        IValue Serialize();
    }
}
