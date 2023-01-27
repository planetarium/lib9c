using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action.Interface
{
    public interface IEventConsumableItemCrafts : IAction
    {
        Address AvatarAddress { get; }
        int EventScheduleId { get; }
        int EventConsumableItemRecipeId { get; }
        int SlotIndex { get; }
    }
}
