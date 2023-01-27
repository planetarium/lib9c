using Libplanet;

namespace Nekoyume.Action.Interface
{
    public interface IEventConsumableItemCrafts : IEventConsumableItemCraftsFamily
    {
        Address AvatarAddress { get; }
        int EventScheduleId { get; }
        int EventConsumableItemRecipeId { get; }
        int SlotIndex { get; }
    }
}
