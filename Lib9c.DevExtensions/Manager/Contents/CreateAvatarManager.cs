using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Lib9c.DevExtensions.Model;
using Nekoyume;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Model.Skill;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.Action;

namespace Lib9c.DevExtensions.Manager.Contents
{
    /// <summary>
    /// Manager class for creating an avatar for testing.
    /// </summary>
    public static class CreateAvatarManager
    {
        /// <summary>
        /// Create an avatar and world state for testing.
        /// </summary>
        /// <param name="ctx">action context</param>
        /// <param name="avatarAddress">avatar address</param>
        /// <param name="states">base world state</param>
        /// <param name="avatarState">avatar state</param>
        /// <returns>world state with dev avatar</returns>
        public static IWorld ExecuteDevExtensions(IActionContext ctx, Address avatarAddress, IWorld states, AvatarState avatarState)
        {
            // prepare for test when executing on editor mode.
            var data = TestbedHelper.LoadData<TestbedCreateAvatar>("TestbedCreateAvatar");

            states = AddRunesForTest(ctx, avatarAddress, states, data.RuneStoneCount);
            states = AddSoulStoneForTest(ctx, avatarAddress, states, data.SoulStoneCount);
            if (data.AddPet)
            {
                states = AddPetsForTest(avatarAddress, states);
            }

            var equipmentSheet = states.GetSheet<EquipmentItemSheet>();
            var recipeSheet = states.GetSheet<EquipmentItemRecipeSheet>();
            var subRecipeSheet = states.GetSheet<EquipmentItemSubRecipeSheetV2>();
            var optionSheet = states.GetSheet<EquipmentItemOptionSheet>();
            var skillSheet = states.GetSheet<SkillSheet>();
            var characterLevelSheet = states.GetSheet<CharacterLevelSheet>();
            var enhancementCostSheet = states.GetSheet<EnhancementCostSheetV2>();
            var materialItemSheet = states.GetSheet<MaterialItemSheet>();
            var random = ctx.GetRandom();

            AddTestItems(ctx, avatarState, random, materialItemSheet);

            avatarState.level = data.Level;
            avatarState.exp = characterLevelSheet[data.Level].Exp;

            foreach (var recipeId in data.FullOptionEquipmentRecipeIds)
            {
                AddFullOptionEquipment(
                    avatarState,
                    random,
                    equipmentSheet,
                    recipeSheet,
                    subRecipeSheet,
                    optionSheet,
                    skillSheet,
                    enhancementCostSheet,
                    recipeId);
            }

            return states;
        }

        private static void AddFullOptionEquipment(
            AvatarState avatarState,
            IRandom random,
            EquipmentItemSheet equipmentSheet,
            EquipmentItemRecipeSheet recipeSheet,
            EquipmentItemSubRecipeSheetV2 subRecipeSheet,
            EquipmentItemOptionSheet optionSheet,
            SkillSheet skillSheet,
            EnhancementCostSheetV2 enhancementCostSheet,
            int recipeId)
        {
            var recipeRow = recipeSheet[recipeId];
            var subRecipeId = recipeRow.SubRecipeIds[1];
            var subRecipeRow = subRecipeSheet[subRecipeId];
            var equipmentRow = equipmentSheet[recipeRow.ResultEquipmentId];

            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                equipmentRow,
                random.GenerateRandomGuid(),
                0L,
                madeWithMimisbrunnrRecipe: subRecipeRow.IsMimisbrunnrSubRecipe ?? false);

            foreach (var option in subRecipeRow.Options)
            {
                var optionRow = optionSheet[option.Id];
                AddOptionToEquipment(equipment, optionRow, skillSheet);
            }

            EnhanceEquipmentToMaxLevel(equipment, enhancementCostSheet, random);

            avatarState.inventory.AddItem(equipment);
        }

        private static void AddOptionToEquipment(Equipment equipment, EquipmentItemOptionSheet.Row optionRow, SkillSheet skillSheet)
        {
            // Add stats.
            if (optionRow.StatType != StatType.NONE)
            {
                var statMap = new DecimalStat(optionRow.StatType, optionRow.StatMax);
                equipment.StatsMap.AddStatAdditionalValue(statMap.StatType, statMap.TotalValue);
                equipment.optionCountFromCombination++;
            }
            // Add skills.
            else
            {
                var skillRow = skillSheet.OrderedList.First(r => r.Id == optionRow.SkillId);
                var skill = SkillFactory.Get(
                    skillRow,
                    optionRow.SkillDamageMax,
                    optionRow.SkillChanceMax,
                    optionRow.StatDamageRatioMax,
                    optionRow.ReferencedStatType);
                if (skill == null)
                {
                    return;
                }

                equipment.Skills.Add(skill);
                equipment.optionCountFromCombination++;
            }
        }

        private static void EnhanceEquipmentToMaxLevel(Equipment equipment, EnhancementCostSheetV2 enhancementCostSheet, IRandom random)
        {
            for (int i = 1; i <= 20; ++i)
            {
                var subType = equipment.ItemSubType;
                var grade = equipment.Grade;
                var costRow = enhancementCostSheet.Values
                    .First(x => x.ItemSubType == subType &&
                                x.Grade == grade &&
                                x.Level == i);
                equipment.LevelUp(random, costRow, true);
            }
        }

        /// <summary>
        /// Add test items to the avatar.
        /// </summary>
        public static void AddTestItems(
            IActionContext ctx,
            AvatarState avatarState,
            IRandom random,
            MaterialItemSheet materialItemSheet)
        {
            var data = TestbedHelper.LoadData<TestbedCreateAvatar>("TestbedCreateAvatar");
            var costumeItemSheet = ctx.PreviousState.GetSheet<CostumeItemSheet>();
            var equipmentItemSheet = ctx.PreviousState.GetSheet<EquipmentItemSheet>();
            var consumableItemSheet = ctx.PreviousState.GetSheet<ConsumableItemSheet>();

            AddItemsForTest(
                avatarState: avatarState,
                random: random,
                costumeItemSheet: costumeItemSheet,
                materialItemSheet: materialItemSheet,
                equipmentItemSheet: equipmentItemSheet,
                consumableItemSheet: consumableItemSheet,
                data.MaterialCount,
                data.TradableMaterialCount,
                data.FoodCount);

            var skillSheet = ctx.PreviousState.GetSheet<SkillSheet>();
            var optionSheet = ctx.PreviousState.GetSheet<EquipmentItemOptionSheet>();

            var items = data.CustomEquipmentItems;
            foreach (var item in items)
            {
                AddCustomEquipment(
                    avatarState: avatarState,
                    random: random,
                    skillSheet: skillSheet,
                    equipmentItemSheet: equipmentItemSheet,
                    equipmentItemOptionSheet: optionSheet,
                    // Set level of equipment here.
                    level: item.Level,
                    // Set recipeId of target equipment here.
                    recipeId: item.Id,
                    // Add optionIds here.
                    item.OptionIds);
            }
        }

        private static IWorld AddRunesForTest(
            IActionContext context,
            Address avatarAddress,
            IWorld states,
            int count = int.MaxValue)
        {
            var runeSheet = states.GetSheet<RuneSheet>();
            foreach (var row in runeSheet.Values)
            {
                var rune = RuneHelper.ToFungibleAssetValue(row, count);
                states = states.MintAsset(context, avatarAddress, rune);
            }
            return states;
        }

        private static IWorld AddSoulStoneForTest(
            IActionContext context,
            Address avatarAddress,
            IWorld states,
            int count = int.MaxValue)
        {
            var petSheet = states.GetSheet<PetSheet>();
            foreach (var row in petSheet.Values)
            {
                var soulStone = Currencies.GetSoulStone(row.SoulStoneTicker) * count;
                states = states.MintAsset(context, avatarAddress, soulStone);
            }
            return states;
        }

        private static IWorld AddPetsForTest(
            Address avatarAddress,
            IWorld states)
        {
            var petSheet = states.GetSheet<PetSheet>();
            foreach (var id in petSheet.Keys)
            {
                var petState = new PetState(id);
                petState.LevelUp();
                var petStateAddress = PetState.DeriveAddress(avatarAddress, id);
                states = states.SetLegacyState(petStateAddress, petState.Serialize());
            }

            return states;
        }

        private static void AddItemsForTest(
            AvatarState avatarState,
            IRandom random,
            CostumeItemSheet costumeItemSheet,
            MaterialItemSheet materialItemSheet,
            EquipmentItemSheet equipmentItemSheet,
            ConsumableItemSheet consumableItemSheet,
            int materialCount,
            int tradableMaterialCount,
            int foodCount)
        {
            foreach (var row in costumeItemSheet.OrderedList)
            {
                avatarState.inventory.AddItem(ItemFactory.CreateCostume(row, random.GenerateRandomGuid()));
            }

            foreach (var row in materialItemSheet.OrderedList)
            {
                avatarState.inventory.AddItem(ItemFactory.CreateMaterial(row), materialCount);

                if (row.ItemSubType == ItemSubType.Hourglass ||
                    row.ItemSubType == ItemSubType.ApStone)
                {
                    avatarState.inventory.AddItem(ItemFactory.CreateTradableMaterial(row), tradableMaterialCount);
                }
            }

            foreach (var row in equipmentItemSheet.OrderedList.Where(row =>
                row.Id > GameConfig.DefaultAvatarWeaponId))
            {
                var itemId = random.GenerateRandomGuid();
                avatarState.inventory.AddItem(ItemFactory.CreateItemUsable(row, itemId, default));
            }

            foreach (var row in consumableItemSheet.OrderedList)
            {
                for (var i = 0; i < foodCount; i++)
                {
                    var itemId = random.GenerateRandomGuid();
                    var consumable = (Consumable)ItemFactory.CreateItemUsable(row, itemId,
                        0, 0);
                    avatarState.inventory.AddItem(consumable);
                }
            }
        }

        private static void AddCustomEquipment(
            AvatarState avatarState,
            IRandom random,
            SkillSheet skillSheet,
            EquipmentItemSheet equipmentItemSheet,
            EquipmentItemOptionSheet equipmentItemOptionSheet,
            int level,
            int recipeId,
            params int[] optionIds
            )
        {
            if (!equipmentItemSheet.TryGetValue(recipeId, out var equipmentRow))
            {
                return;
            }

            var itemId = random.GenerateRandomGuid();
            var equipment = (Equipment)ItemFactory.CreateItemUsable(equipmentRow, itemId, 0, level);
            var optionRows = new List<EquipmentItemOptionSheet.Row>();
            foreach (var optionId in optionIds)
            {
                if (!equipmentItemOptionSheet.TryGetValue(optionId, out var optionRow))
                {
                    continue;
                }
                optionRows.Add(optionRow);
            }

            AddOption(skillSheet, equipment, optionRows, random);

            avatarState.inventory.AddItem(equipment);
        }

        private static HashSet<int> AddOption(
            SkillSheet skillSheet,
            Equipment equipment,
            IEnumerable<EquipmentItemOptionSheet.Row> optionRows,
            IRandom random)
        {
            var optionIds = new HashSet<int>();

            foreach (var optionRow in optionRows.OrderBy(r => r.Id))
            {
                if (optionRow.StatType != StatType.NONE)
                {
                    var stat = CombinationEquipment5.GetStat(optionRow, random);
                    equipment.StatsMap.AddStatAdditionalValue(stat.StatType, stat.BaseValue);
                }
                else
                {
                    var skill = CombinationEquipment5.GetSkill(optionRow, skillSheet, random);
                    if (!(skill is null))
                    {
                        equipment.Skills.Add(skill);
                    }
                }

                optionIds.Add(optionRow.Id);
            }

            return optionIds;
        }
    }
}
