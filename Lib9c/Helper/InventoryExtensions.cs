using System.Linq;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    public static class InventoryExtensions
    {
        public static int GetEquippedFullCostumeOrArmorId(this Inventory inventory)
        {
            foreach (var costume in inventory.Costumes
                         .Where(e =>
                             e.ItemSubType == ItemSubType.FullCostume &&
                             e.Equipped))
            {
                return costume.Id;
            }

            foreach (var armor in inventory.Equipments
                         .Where(e =>
                             e.ItemSubType == ItemSubType.Armor &&
                             e.Equipped))
            {
                return armor.Id;
            }

            return GameConfig.DefaultAvatarArmorId;
        }

        public static long UseActionPoint(
            this Inventory inventory,
            long originalAp,
            int requiredAp,
            bool chargeAp,
            MaterialItemSheet materialItemSheet,
            long blockIndex)
        {
            if (originalAp < requiredAp)
            {
                switch (chargeAp)
                {
                    case true:
                        var row = materialItemSheet
                            .OrderedList!
                            .First(r => r.ItemSubType == ItemSubType.ApStone);
                        if (!inventory.RemoveFungibleItem(row.ItemId, blockIndex))
                        {
                            throw new NotEnoughMaterialException("not enough ap stone.");
                        }

                        originalAp = DailyReward.ActionPointMax;
                        break;
                    case false:
                        throw new NotEnoughActionPointException("");
                }
            }

            originalAp -= requiredAp;
            return originalAp;
        }

        /// <summary>
        /// Create and Add item.
        /// </summary>
        /// <param name="inventory"><see cref="Inventory"/>></param>
        /// <param name="itemRow"><see cref="ItemSheet.Row"/></param>
        /// <param name="quantity">create item count</param>
        /// <param name="tradable">item is tradable</param>
        /// <param name="random"><see cref="IRandom"/>></param>
        public static void MintItem(this Inventory inventory, ItemSheet.Row itemRow, int quantity,
            bool tradable, IRandom random)
        {
            if (itemRow is MaterialItemSheet.Row materialRow)
            {
                var item = tradable
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateMaterial(materialRow);
                inventory.AddItem(item, quantity);
            }
            else
            {
                foreach (var _ in Enumerable.Range(0, quantity))
                {
                    var item = ItemFactory.CreateItem(itemRow, random);
                    inventory.AddItem(item);
                }
            }
        }
    }
}
