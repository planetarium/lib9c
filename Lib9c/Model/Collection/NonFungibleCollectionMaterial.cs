using System;
using Bencodex.Types;
using Lib9c.Action;
using Lib9c.Model.Item;
using Lib9c.Model.State;
using Lib9c.TableData;
using Lib9c.TableData.Item;

namespace Lib9c.Model.Collection
{
    public class NonFungibleCollectionMaterial : ICollectionMaterial
    {
        public MaterialType Type => MaterialType.NonFungible;
        public int ItemId { get; set; }
        public int ItemCount { get; set; }
        public Guid NonFungibleId { get; set; }
        public int Level { get; set; }
        public bool SkillContains { get; set; }

        public IValue Bencoded => List.Empty
            .Add((int)Type)
            .Add(ItemId)
            .Add(ItemCount)
            .Add(NonFungibleId.Serialize())
            .Add(Level)
            .Add(SkillContains.Serialize());

        public NonFungibleCollectionMaterial(List serialized)
        {
            ItemId = (Integer)serialized[1];
            ItemCount = (Integer)serialized[2];
            NonFungibleId = serialized[3].ToGuid();
            Level = (Integer)serialized[4];
            SkillContains = serialized[5].ToBoolean();
        }

        public NonFungibleCollectionMaterial()
        {
            ItemCount = 1;
        }

        /// <summary>
        /// Burns the specified material from the inventory based on the item type.
        /// </summary>
        /// <param name="itemRow">The <see cref="ItemSheet.Row"/> object representing the item.</param>
        /// <param name="inventory">The <see cref="Inventory"/> object representing the player's inventory.</param>
        /// <param name="materialInfo">The <see cref="CollectionSheet.RequiredMaterial"/> object representing the material info.</param>
        /// <param name="blockIndex">The block index where the burn operation is taking place.</param>
        /// <exception cref="ItemDoesNotExistException">Thrown when the material item does not exist in the inventory.</exception>
        /// <exception cref="InvalidItemTypeException">Thrown when the item type is not supported by <see cref="NonFungibleCollectionMaterial"/>.</exception>
        public void BurnMaterial(ItemSheet.Row itemRow, Inventory inventory,
            CollectionSheet.RequiredMaterial materialInfo, long blockIndex)
        {
            switch (itemRow.ItemType)
            {
                case ItemType.Costume:
                case ItemType.Equipment:
                    if (inventory.TryGetNonFungibleItem(NonFungibleId,
                            out INonFungibleItem materialItem, blockIndex) &&
                        materialInfo.Validate(materialItem))
                    {
                        inventory.RemoveNonFungibleItem(materialItem);
                    }
                    else
                    {
                        throw new ItemDoesNotExistException($"failed to load {itemRow.ItemType}");
                    }

                    break;
                case ItemType.Consumable:
                case ItemType.Material:
                default:
                    throw new InvalidItemTypeException(
                        $"{nameof(NonFungibleCollectionMaterial)} does not support {itemRow.ItemType}");
            }
        }
    }
}
