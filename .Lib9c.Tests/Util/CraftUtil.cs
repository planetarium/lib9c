namespace Lib9c.Tests.Util
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using static Lib9c.SerializeKeys;

    public static class CraftUtil
    {
        public static IWorld PrepareCombinationSlot(
            IWorld world,
            Address avatarAddress,
            int slotIndex
        )
        {
            var slotAddress = avatarAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                slotIndex));
            var slotState = new CombinationSlotState(
                slotAddress,
                // CombinationEquipment: 3
                // CombinationConsumable: 6
                // ItemEnhancement: 9
                GameConfig.RequireClearedStageLevel.ItemEnhancementAction
            );
            return LegacyModule.SetState(world, slotAddress, slotState.Serialize());
        }

        public static IWorld AddMaterialsToInventory(
            IWorld world,
            TableSheets tableSheets,
            Address avatarAddress,
            IEnumerable<EquipmentItemSubRecipeSheet.MaterialInfo> materialList,
            IRandom random
        )
        {
            var avatarState = AvatarModule.GetAvatarState(world, avatarAddress);
            foreach (var material in materialList)
            {
                var materialRow = tableSheets.MaterialItemSheet[material.Id];
                var materialItem = ItemFactory.CreateItem(materialRow, random);
                avatarState.inventory.AddItem(materialItem, material.Count);
            }

            return AvatarModule.SetAvatarState(
                world,
                avatarAddress,
                avatarState,
                false,
                true,
                false,
                false);
        }

        public static IWorld UnlockStage(
            IWorld world,
            TableSheets tableSheets,
            Address avatarAddress,
            int stage
        )
        {
            var worldInformation = new WorldInformation(
                0,
                tableSheets.WorldSheet,
                Math.Max(stage, GameConfig.RequireClearedStageLevel.ItemEnhancementAction)
            );
            return AvatarModule.SetWorldInformation(world, avatarAddress, worldInformation);
        }
    }
}
