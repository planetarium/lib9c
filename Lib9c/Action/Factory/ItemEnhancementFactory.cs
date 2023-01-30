using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Libplanet;
using Nekoyume.Action.Interface;

namespace Nekoyume.Action.Factory
{
    public static class ItemEnhancementFactory
    {
        private static (Type type, string actionTypeIdentifier)[] _tuples;

        private static (Type type, string actionTypeIdentifier)[] Tuples
        {
            get
            {
                _tuples ??= FactoryUtils.GetTuples<IItemEnhancement>()
                    .Concat(FactoryUtils.GetTuples<IItemEnhancementV2>())
                    .ToArray();

                return _tuples;
            }
        }

        #region By block index

        public static IItemEnhancement Create(
            long blockIndex,
            Guid itemId,
            IEnumerable<Guid> materialIds,
            Address avatarAddress,
            int slotIndex)
        {
            if (blockIndex < 0L)
            {
                throw new NotMatchFoundException(
                    typeof(IItemEnhancement),
                    blockIndex);
            }

            var actionTypeIdentifier = string.Empty;
            if (blockIndex <= ItemEnhancement0.ObsoleteIndex)
            {
                actionTypeIdentifier = ItemEnhancement0.ActionTypeIdentifier;
            }

            if (string.IsNullOrEmpty(actionTypeIdentifier))
            {
                throw new NotMatchFoundException(
                    typeof(IItemEnhancement),
                    blockIndex);
            }

            return Create(
                actionTypeIdentifier,
                itemId,
                materialIds,
                avatarAddress,
                slotIndex);
        }

        public static IItemEnhancementV2 Create(
            long blockIndex,
            Guid itemId,
            Guid materialId,
            Address avatarAddress,
            int slotIndex)
        {
            // NOTE: This condition is not strict.
            //       Because any actions does not have activation block index.
            //       And the `ItemEnhancement0.ObsoleteIndex` is same with
            //       `ItemEnhancement8.ObsoleteIndex`.
            if (blockIndex < 0L)
            {
                throw new NotMatchFoundException(
                    typeof(IItemEnhancementV2),
                    blockIndex);
            }

            string actionTypeIdentifier;
            // NOTE: ItemEnhancement(1~8).ObsoleteIndex are same.
            if (blockIndex <= ItemEnhancement8.ObsoleteIndex)
            {
                actionTypeIdentifier = ItemEnhancement8.ActionTypeIdentifier;
            }
            else if (blockIndex <= ItemEnhancement9.ObsoleteIndex)
            {
                actionTypeIdentifier = ItemEnhancement9.ActionTypeIdentifier;
            }
            // FIXME: The ItemEnhancement10 action obsoleted via other way.
            //        When the action is obsoleted, the following code should be uncommented.
            // else if (blockIndex <= ItemEnhancement10.ObsoleteIndex)
            // {
            //     actionTypeIdentifier = ItemEnhancement10.ActionTypeIdentifier;
            // }
            else
            {
                actionTypeIdentifier = ItemEnhancement.ActionTypeIdentifier;
            }

            return Create(
                actionTypeIdentifier,
                itemId,
                materialId,
                avatarAddress,
                slotIndex);
        }

        #endregion

        #region By action type identifier

        public static IItemEnhancement Create(
            string actionTypeIdentifier,
            Guid itemId,
            IEnumerable<Guid> materialIds,
            Address avatarAddress,
            int slotIndex)
        {
            var action = FactoryUtils.CreateInstance<IItemEnhancement>(
                actionTypeIdentifier,
                Tuples);
            var type = action.GetType();
            FactoryUtils.SetField(action, type, nameof(itemId), itemId);
            FactoryUtils.SetField(action, type, nameof(materialIds), materialIds);
            FactoryUtils.SetField(action, type, nameof(avatarAddress), avatarAddress);
            FactoryUtils.SetField(action, type, nameof(slotIndex), slotIndex);

            return action;
        }

        public static IItemEnhancementV2 Create(
            string actionTypeIdentifier,
            Guid itemId,
            Guid materialId,
            Address avatarAddress,
            int slotIndex)
        {
            var action = FactoryUtils.CreateInstance<IItemEnhancementV2>(
                actionTypeIdentifier,
                Tuples);
            var type = action.GetType();
            FactoryUtils.SetField(action, type, nameof(itemId), itemId);
            FactoryUtils.SetField(action, type, nameof(materialId), materialId);
            FactoryUtils.SetField(action, type, nameof(avatarAddress), avatarAddress);
            FactoryUtils.SetField(action, type, nameof(slotIndex), slotIndex);

            return action;
        }

        #endregion
    }
}
