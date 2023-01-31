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

        #region By block index

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

            // NOTE: There is only one type of EventConsumableItemCrafts.
            return new EventConsumableItemCrafts
            {
                AvatarAddress = avatarAddr,
                EventScheduleId = eventScheduleId,
                EventConsumableItemRecipeId = eventConsumableItemRecipeId,
                SlotIndex = slotIndex,
            };
        }

        #endregion

        #region By action type identifier

        public static IEventConsumableItemCrafts Create(
            string actionTypeIdentifier,
            Address avatarAddr,
            int eventScheduleId,
            int eventConsumableItemRecipeId,
            int slotIndex)
        {
            if (string.IsNullOrEmpty(actionTypeIdentifier))
            {
                throw new NotMatchFoundException(
                    typeof(IEventConsumableItemCrafts),
                    actionTypeIdentifier);
            }

            var (type, _) = Tuples.FirstOrDefault(tuple =>
                tuple.actionType == actionTypeIdentifier);
            if (type is null)
            {
                throw new NotMatchFoundException(
                    typeof(IEventMaterialItemCrafts),
                    actionTypeIdentifier);
            }

            var action = Activator.CreateInstance(type) as IEventConsumableItemCrafts;
            if (action is null)
            {
                throw new NotMatchFoundException(
                    typeof(IEventConsumableItemCrafts),
                    actionTypeIdentifier);
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
                        $"{actionTypeIdentifier} is not supported.",
                        new NotImplementedException());
            }
        }

        #endregion
    }
}
