using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action.Interface
{
    public interface ICombinationConsumableFamily : IAction
    {
    }

    public interface ICombinationConsumable : ICombinationConsumableFamily
    {
        Address AvatarAddress { get; }
        int SlotIndex { get; }
        int RecipeId { get; }
    }
}
