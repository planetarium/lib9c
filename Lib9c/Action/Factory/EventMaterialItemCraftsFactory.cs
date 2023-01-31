using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Action.Interface;

namespace Nekoyume.Action.Factory
{
    public static class EventMaterialItemCraftsFactory
    {
        private static (Type type, string actionType)[] _tuples;

        private static (Type type, string actionType)[] Tuples =>
            _tuples ??= FactoryUtils.GetTuples<IEventMaterialItemCrafts>();

        public static IEventMaterialItemCrafts Create(
            long blockIndex,
            Address avatarAddr,
            int eventScheduleId,
            int eventMaterialItemRecipeId,
            Dictionary<int, int> materialsToUse)
        {
            if (blockIndex < 0L)
            {
                throw new NotMatchFoundException(
                    typeof(IEventMaterialItemCrafts),
                    blockIndex);
            }

            // NOTE: There is only one type of EventMaterialItemCrafts.
            return new EventMaterialItemCrafts
            {
                AvatarAddress = avatarAddr,
                EventScheduleId = eventScheduleId,
                EventMaterialItemRecipeId = eventMaterialItemRecipeId,
                MaterialsToUse = materialsToUse,
            };
        }

        public static IEventMaterialItemCrafts Create(
            string actionTypeIdentifier,
            Address avatarAddr,
            int eventScheduleId,
            int eventMaterialItemRecipeId,
            Dictionary<int, int> materialsToUse)
        {
            if (string.IsNullOrEmpty(actionTypeIdentifier))
            {
                throw new NotMatchFoundException(
                    typeof(IEventMaterialItemCrafts),
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

            var action = Activator.CreateInstance(type) as IEventMaterialItemCrafts;
            if (action is null)
            {
                throw new NotMatchFoundException(
                    typeof(IEventMaterialItemCrafts),
                    actionTypeIdentifier);
            }

            switch (action)
            {
                case EventMaterialItemCrafts a:
                    a.AvatarAddress = avatarAddr;
                    a.EventScheduleId = eventScheduleId;
                    a.EventMaterialItemRecipeId = eventMaterialItemRecipeId;
                    a.MaterialsToUse = materialsToUse;
                    return a;
                default:
                    throw new NotMatchFoundException(
                        $"{actionTypeIdentifier} is not supported.",
                        new NotImplementedException());
            }
        }
    }
}
