using System;
using System.Linq;
using Libplanet;
using Nekoyume.Action.Interface;

namespace Nekoyume.Action.Factory
{
    public static class EventConsumableItemCraftsFactory
    {
        private static (Type type, string actionType)[] _tuples;

        private static (Type type, string actionType)[] Tuples =>
            _tuples ??= FactoryUtils.GetTuples<IEventConsumableItemCrafts>();

        public static IEventConsumableItemCrafts Create(
            long blockIndex,
            Address avatarAddr,
            int eventScheduleId,
            int eventConsumableItemRecipeId,
            int slotIndex)
        {
            if (blockIndex < 0L)
            {
                throw new NotMatchFoundException(
                    typeof(IEventConsumableItemCrafts),
                    blockIndex);
            }

            return new EventConsumableItemCrafts
            {
                AvatarAddress = avatarAddr,
                EventScheduleId = eventScheduleId,
                EventConsumableItemRecipeId = eventConsumableItemRecipeId,
                SlotIndex = slotIndex,
            };
        }

        public static IEventConsumableItemCrafts Create(
            string actionType,
            Address avatarAddr,
            int eventScheduleId,
            int eventConsumableItemRecipeId,
            int slotIndex)
        {
            if (string.IsNullOrEmpty(actionType))
            {
                throw new NotMatchFoundException(
                    typeof(IEventConsumableItemCrafts),
                    actionType);
            }

            var (type, _) = Tuples.FirstOrDefault(tuple => tuple.actionType == actionType);
            if (type is null)
            {
                throw new NotMatchFoundException(
                    typeof(IEventMaterialItemCrafts),
                    actionType);
            }

            var action = Activator.CreateInstance(type) as IEventConsumableItemCrafts;
            if (action is null)
            {
                throw new NotMatchFoundException(
                    typeof(IEventConsumableItemCrafts),
                    actionType);
            }

            switch (action)
            {
                case EventConsumableItemCrafts a:
                    a.AvatarAddress = avatarAddr;
                    a.EventScheduleId = eventScheduleId;
                    a.EventConsumableItemRecipeId = eventConsumableItemRecipeId;
                    a.SlotIndex = slotIndex;
                    return a;
                default:
                    throw new NotMatchFoundException(
                        $"{actionType} is not supported.",
                        new NotImplementedException());
            }
        }
    }
}
