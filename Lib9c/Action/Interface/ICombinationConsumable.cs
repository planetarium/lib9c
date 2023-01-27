using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action.Interface
{
    public interface ICombinationConsumable : IAction
    {
        Address AvatarAddress { get; }
        int SlotIndex { get; }
        int RecipeId { get; }
    }
}
