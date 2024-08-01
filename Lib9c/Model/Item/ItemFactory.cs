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
    public static class ItemFactory
    {
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

        public static Costume CreateCostume(CostumeItemSheet.Row row, Guid itemId)
        {
            return new Costume(row, itemId);
        }

        public static Material CreateMaterial(MaterialItemSheet sheet, int itemId)
        {
            return sheet.TryGetValue(itemId, out var itemData)
                ? CreateMaterial(itemData)
                : null;
        }

        public static Material CreateMaterial(MaterialItemSheet.Row row) => new Material(row);

        public static TradableMaterial CreateTradableMaterial(MaterialItemSheet.Row row)
            => new TradableMaterial(row);

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

        public static int SelectIconId(
            int iconId, bool isRandom, EquipmentItemSheet.Row equipmentRow, int relationship,
            CustomEquipmentCraftIconSheet iconSheet, IRandom random
        )
        {
            // Validate and select Icon ID
            int selectedIconId;

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

                selectedIconId = iconSelector.Select(1).First().IconId;
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

            return selectedIconId;
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

        public static Skill.Skill SelectSkill(
            ItemSubType itemSubType,
            CustomEquipmentCraftRecipeSkillSheet recipeSkillSheet,
            EquipmentItemOptionSheet itemOptionSheet,
            SkillSheet skillSheet,
            IRandom random
        )
        {
            var skillSelector =
                new WeightedSelector<CustomEquipmentCraftRecipeSkillSheet.Row>(random);
            foreach (var sr in recipeSkillSheet.Values
                         .Where(row => row.ItemSubType == itemSubType))
            {
                skillSelector.Add(sr, sr.Ratio);
            }

            var itemOptionId = skillSelector.Select(1).First().ItemOptionId;
            var skillOptionRow = itemOptionSheet.Values.First(row => row.Id == itemOptionId);
            var skillRow = skillSheet.Values.First(row => row.Id == skillOptionRow.SkillId);

            var hasStatDamageRatio = skillOptionRow.StatDamageRatioMin != default &&
                                     skillOptionRow.StatDamageRatioMax != default;
            var statDamageRatio = hasStatDamageRatio
                ? random.Next(skillOptionRow.StatDamageRatioMin,
                    skillOptionRow.StatDamageRatioMax + 1)
                : default;
            var refStatType = hasStatDamageRatio
                ? skillOptionRow.ReferencedStatType
                : StatType.NONE;

            return SkillFactory.Get(
                skillRow,
                random.Next(skillOptionRow.SkillDamageMin, skillOptionRow.SkillDamageMax + 1),
                random.Next(skillOptionRow.SkillChanceMin, skillOptionRow.SkillChanceMax + 1),
                statDamageRatio,
                refStatType
            );
        }

        public static Equipment CreateCustomEquipment(
            IRandom random,
            int iconId,
            EquipmentItemSheet.Row equipmentRow,
            long endBlockIndex,
            int avatarLevel,
            CustomEquipmentCraftRelationshipSheet.Row relationshipRow,
            CustomEquipmentCraftOptionSheet.Row optionRow,
            Skill.Skill skill
        )
        {
            var guid = random.GenerateRandomGuid();
            var equipment = (Equipment)CreateItemUsable(equipmentRow, guid, endBlockIndex);

            equipment.IconId = iconId;

            // Set Substats
            var totalCp = (decimal)random.Next(
                relationshipRow.MinCp,
                relationshipRow.MaxCp + 1
            );

            foreach (var option in optionRow.SubStatData)
            {
                equipment.StatsMap.AddStatAdditionalValue(option.StatType,
                    CPHelper.ConvertCpToStat(option.StatType,
                        totalCp * option.Ratio / optionRow.TotalOptionRatio,
                        avatarLevel)
                );
            }

            // Set skill
            equipment.Skills.Add(skill);

            return equipment;
        }

        public static ItemBase Deserialize(Dictionary serialized)
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
