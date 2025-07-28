using System;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Action.Exceptions.CustomEquipmentCraft;
using Nekoyume.Battle;
using Nekoyume.Exceptions;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.Model.State;
using Nekoyume.TableData.CustomEquipmentCraft;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.Item
{
    /// <summary>
    /// Factory class for creating and deserializing items.
    /// Supports both Dictionary and List serialization formats for backward compatibility.
    /// </summary>
    public static class ItemFactory
    {
        /// <summary>
        /// Creates an item from the given row data.
        /// </summary>
        /// <param name="row">The item sheet row data</param>
        /// <param name="random">Random number generator</param>
        /// <returns>The created item</returns>
        public static ItemBase CreateItem(ItemSheet.Row row, IRandom random)
        {
            switch (row)
            {
                case CostumeItemSheet.Row costumeRow:
                    return CreateCostume(costumeRow, random.GenerateRandomGuid());
                case MaterialItemSheet.Row materialRow:
                    return CreateMaterial(materialRow);
                default:
                    return CreateItemUsable(row, random.GenerateRandomGuid(), 0);
            }
        }

        /// <summary>
        /// Creates a costume item.
        /// </summary>
        /// <param name="row">The costume item sheet row data</param>
        /// <param name="itemId">The unique ID for the costume</param>
        /// <returns>The created costume</returns>
        public static Costume CreateCostume(CostumeItemSheet.Row row, Guid itemId)
        {
            return new Costume(row, itemId);
        }

        /// <summary>
        /// Creates a material item from the sheet by ID.
        /// </summary>
        /// <param name="sheet">The material item sheet</param>
        /// <param name="itemId">The material item ID</param>
        /// <returns>The created material, or null if not found</returns>
        public static Material CreateMaterial(MaterialItemSheet sheet, int itemId)
        {
            return sheet.TryGetValue(itemId, out var itemData)
                ? CreateMaterial(itemData)
                : null;
        }

        /// <summary>
        /// Creates a material item from the given row data.
        /// </summary>
        /// <param name="row">The material item sheet row data</param>
        /// <returns>The created material</returns>
        public static Material CreateMaterial(MaterialItemSheet.Row row) => new Material(row);

        /// <summary>
        /// Creates a tradable material item from the given row data.
        /// </summary>
        /// <param name="row">The material item sheet row data</param>
        /// <returns>The created tradable material</returns>
        public static TradableMaterial CreateTradableMaterial(MaterialItemSheet.Row row)
            => new TradableMaterial(row);

        /// <summary>
        /// Creates a usable item (consumable or equipment) from the given row data.
        /// </summary>
        /// <param name="itemRow">The item sheet row data</param>
        /// <param name="id">The unique ID for the item</param>
        /// <param name="requiredBlockIndex">The required block index for using the item</param>
        /// <param name="level">The level of the equipment (default: 0)</param>
        /// <param name="madeWithMimisbrunnrRecipe">Whether the item was made with Mimisbrunnr recipe (default: false)</param>
        /// <returns>The created usable item</returns>
        public static ItemUsable CreateItemUsable(ItemSheet.Row itemRow, Guid id,
            long requiredBlockIndex, int level = 0, bool madeWithMimisbrunnrRecipe = false)
        {
            Equipment equipment = null;

            switch (itemRow.ItemSubType)
            {
                // Consumable
                case ItemSubType.Food:
                    return new Consumable((ConsumableItemSheet.Row) itemRow, id, requiredBlockIndex);
                // Equipment
                case ItemSubType.Weapon:
                    equipment = new Weapon((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Armor:
                    equipment = new Armor((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Belt:
                    equipment = new Belt((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Necklace:
                    equipment = new Necklace((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Ring:
                    equipment = new Ring((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Aura:
                    equipment = new Aura((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex,
                        madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Grimoire:
                    equipment = new Grimoire((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        itemRow.Id.ToString(CultureInfo.InvariantCulture));
            }

            for (int i = 0; i < level; ++i)
            {
                equipment.LevelUpV1();
            }

            return equipment;
        }

        public static ItemUsable CreateItemUsableV2(ItemSheet.Row itemRow, Guid id,
            long requiredBlockIndex, int level,
            IRandom random, EnhancementCostSheetV2.Row row, bool isGreatSuccess, bool madeWithMimisbrunnrRecipe = false)
        {
            Equipment equipment = null;

            switch (itemRow.ItemSubType)
            {
                // Consumable
                case ItemSubType.Food:
                    return new Consumable((ConsumableItemSheet.Row) itemRow, id, requiredBlockIndex);
                // Equipment
                case ItemSubType.Weapon:
                    equipment = new Weapon((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Armor:
                    equipment = new Armor((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Belt:
                    equipment = new Belt((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Necklace:
                    equipment = new Necklace((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                case ItemSubType.Ring:
                    equipment = new Ring((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        itemRow.Id.ToString(CultureInfo.InvariantCulture));
            }

            for (int i = 0; i < level; ++i)
            {
                equipment.LevelUp(random, row, isGreatSuccess);
            }

            return equipment;
        }

        public static (int, bool) SelectIconId(
            int iconId, bool isRandom, EquipmentItemSheet.Row equipmentRow, int relationship,
            CustomEquipmentCraftIconSheet iconSheet, IRandom random
        )
        {
            // Validate and select Icon ID
            int selectedIconId;
            var isRandomOnlyIcon = false;

            if (isRandom)
            {
                // Random icon
                var iconSelector = new WeightedSelector<CustomEquipmentCraftIconSheet.Row>(random);
                var iconRows = iconSheet.Values
                    .Where(row =>
                        row.RequiredRelationship <= relationship &&
                        row.ItemSubType == equipmentRow.ItemSubType
                    );
                foreach (var row in iconRows)
                {
                    iconSelector.Add(row, row.Ratio);
                }

                var selectedIcon = iconSelector.Select(1).First();
                selectedIconId = selectedIcon.IconId;
                isRandomOnlyIcon = selectedIcon.RandomOnly;
            }
            else
            {
                // Selected icon
                var iconRow = iconSheet.Values.FirstOrDefault(row => row.IconId == iconId);
                if (iconRow is null)
                {
                    throw new InvalidActionFieldException($"Icon ID {iconId} is not valid.");
                }

                if (iconRow.RequiredRelationship > relationship)
                {
                    throw new NotEnoughRelationshipException(
                        $"Relationship {relationship} is less than required relationship {iconRow.RequiredRelationship} to use icon {iconId}"
                    );
                }

                if (iconRow.RandomOnly)
                {
                    throw new RandomOnlyIconException(iconRow.IconId);
                }

                selectedIconId = iconId;
            }

            return (selectedIconId, isRandomOnlyIcon);
        }

        public static CustomEquipmentCraftOptionSheet.Row SelectOption(
            ItemSubType itemSubType,
            CustomEquipmentCraftOptionSheet optionSheet,
            IRandom random
        )
        {
            var optionSelector = new WeightedSelector<CustomEquipmentCraftOptionSheet.Row>(random);
            foreach (var opt in optionSheet.Values
                         .Where(row => row.ItemSubType == itemSubType))
            {
                optionSelector.Add(opt, opt.Ratio);
            }

            return optionSelector.Select(1).First();
        }

        /// <summary>
        /// Deserializes an item from serialized data.
        /// Supports both Dictionary and List formats for backward compatibility.
        /// </summary>
        /// <param name="serialized">The serialized item data</param>
        /// <returns>The deserialized item</returns>
        /// <exception cref="ArgumentException">Thrown when the serialization format is not supported</exception>
        public static ItemBase Deserialize(IValue serialized)
        {
            switch (serialized)
            {
                case Dictionary dict:
                    return DeserializeFromDictionary(dict);
                case List list:
                    return DeserializeFromList(list);
                default:
                    throw new ArgumentException($"Unsupported serialization format: {serialized.GetType()}");
            }
        }

        /// <summary>
        /// Deserializes an item from Dictionary format (legacy support).
        /// </summary>
        /// <param name="serialized">The serialized item data in Dictionary format</param>
        /// <returns>The deserialized item</returns>
        /// <exception cref="ArgumentException">Thrown when the item cannot be deserialized</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the item type is not supported</exception>
        private static ItemBase DeserializeFromDictionary(Dictionary serialized)
        {
            if (serialized.TryGetValue((Text) "item_type", out var type) &&
                serialized.TryGetValue((Text) "item_sub_type", out var subType))
            {
                var itemType = type.ToEnum<ItemType>();
                var itemSubType = subType.ToEnum<ItemSubType>();

                switch (itemType)
                {
                    case ItemType.Consumable:
                        return new Consumable(serialized);
                    case ItemType.Costume:
                        return new Costume(serialized);
                    case ItemType.Equipment:
                        switch (itemSubType)
                        {
                            case ItemSubType.Weapon:
                                return new Weapon(serialized);
                            case ItemSubType.Armor:
                                return new Armor(serialized);
                            case ItemSubType.Belt:
                                return new Belt(serialized);
                            case ItemSubType.Necklace:
                                return new Necklace(serialized);
                            case ItemSubType.Ring:
                                return new Ring(serialized);
                            case ItemSubType.Aura:
                                return new Aura(serialized);
                            case ItemSubType.Grimoire:
                                return new Grimoire(serialized);
                        }
                        break;
                    case ItemType.Material:
                        if (serialized.ContainsKey(RequiredBlockIndexKey))
                        {
                            return new TradableMaterial(serialized);
                        }
                        else
                        {
                            return new Material(serialized);
                        }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(itemType));
                }
            }

            throw new ArgumentException($"Can't Deserialize Item {serialized}");
        }

        /// <summary>
        /// Deserializes an item from List format (new format).
        /// </summary>
        /// <param name="serialized">The serialized item data in List format</param>
        /// <returns>The deserialized item</returns>
        /// <exception cref="ArgumentException">Thrown when the list has insufficient elements</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the item type or sub type is not supported</exception>
        private static ItemBase DeserializeFromList(List serialized)
        {
            if (serialized.Count < 3)
            {
                throw new ArgumentException($"List must have at least 2 elements for item type and sub type: {serialized}");
            }
            var itemType = (ItemType)serialized[2].ToInteger();
            var itemSubType = (ItemSubType)serialized[3].ToInteger();

            switch (itemType)
            {
                case ItemType.Consumable:
                    return new Consumable(serialized);
                case ItemType.Costume:
                    return new Costume(serialized);
                case ItemType.Equipment:
                    switch (itemSubType)
                    {
                        case ItemSubType.Weapon:
                            return new Weapon(serialized);
                        case ItemSubType.Armor:
                            return new Armor(serialized);
                        case ItemSubType.Belt:
                            return new Belt(serialized);
                        case ItemSubType.Necklace:
                            return new Necklace(serialized);
                        case ItemSubType.Ring:
                            return new Ring(serialized);
                        case ItemSubType.Aura:
                            return new Aura(serialized);
                        case ItemSubType.Grimoire:
                            return new Grimoire(serialized);
                        default:
                            throw new ArgumentOutOfRangeException(nameof(itemSubType));
                    }
                case ItemType.Material:
                    // Check if it's a TradableMaterial by looking for RequiredBlockIndex
                    if (serialized.Count == 8) // base fields (7) + requiredBlockIndex (1)
                    {
                        return new TradableMaterial(serialized);
                    }
                    else
                    {
                        return new Material(serialized);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType));
            }
        }
        private static ItemSheet.Row DeserializeRow(Dictionary serialized)
        {
            var itemSubType =
                (ItemSubType) Enum.Parse(typeof(ItemSubType), (Text) serialized["item_sub_type"]);
            switch (itemSubType)
            {
                // Consumable
                case ItemSubType.Food:
                    return new ConsumableItemSheet.Row(serialized);
                // Costume
                case ItemSubType.EarCostume:
                case ItemSubType.EyeCostume:
                case ItemSubType.FullCostume:
                case ItemSubType.HairCostume:
                case ItemSubType.TailCostume:
                case ItemSubType.Title:
                    return new CostumeItemSheet.Row(serialized);
                // Equipment
                case ItemSubType.Weapon:
                case ItemSubType.Armor:
                case ItemSubType.Belt:
                case ItemSubType.Necklace:
                case ItemSubType.Ring:
                    return new EquipmentItemSheet.Row(serialized);
                // Material
                case ItemSubType.EquipmentMaterial:
                case ItemSubType.FoodMaterial:
                case ItemSubType.MonsterPart:
                case ItemSubType.NormalMaterial:
                case ItemSubType.Hourglass:
                case ItemSubType.ApStone:
                    return new MaterialItemSheet.Row(serialized);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
