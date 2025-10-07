using Bencodex.Types;
using Lib9c.Action;
using Lib9c.Model.Item;
using Lib9c.TableData.Item;

namespace Lib9c.Model.Collection
{
    public class FungibleCollectionMaterial : ICollectionMaterial
    {
        public MaterialType Type => MaterialType.Fungible;

        public int ItemId { get; set; }

        public int ItemCount { get; set; }

        public IValue Bencoded => List.Empty
            .Add((int) Type)
            .Add(ItemId)
            .Add(ItemCount);

        public FungibleCollectionMaterial(List serialized)
        {
            ItemId = (Integer)serialized[1];
            ItemCount = (Integer)serialized[2];
        }

        public FungibleCollectionMaterial(IValue bencoded) : this((List) bencoded)
        {
        }

        public FungibleCollectionMaterial()
        {
        }

        /// <summary>
        /// Burns the material from the inventory based on the given item row, inventory, and block index.
        /// </summary>
        /// <param name="itemRow">The item row from the item sheet.</param>
        /// <param name="inventory">The inventory object.</param>
        /// <param name="blockIndex">The block index.</param>
        public void BurnMaterial(ItemSheet.Row itemRow, Inventory inventory, long blockIndex)
        {
            switch (itemRow.ItemType)
            {
                case ItemType.Consumable:
                    if (!inventory.RemoveConsumable(ItemId, blockIndex, ItemCount))
                    {
                        throw new ItemDoesNotExistException(
                            "failed to load consumable from inventory");
                    }

                    break;
                case ItemType.Material:
                    if (!inventory.RemoveMaterial(ItemId, blockIndex, ItemCount))
                    {
                        throw new ItemDoesNotExistException(
                            "failed to load material from inventory");
                    }

                    break;
                case ItemType.Costume:
                case ItemType.Equipment:
                default:
                    throw new InvalidItemTypeException(
                        $"{nameof(FungibleCollectionMaterial)} does not support {itemRow.ItemType}");
            }
        }
    }
}
