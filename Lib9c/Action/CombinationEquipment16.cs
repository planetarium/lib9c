using System.Linq;
using Libplanet.Action;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Pet;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1711
    /// </summary>
    public static class CombinationEquipment16
    {
        public const string AvatarAddressKey = "a";
        public const string SlotIndexKey = "s";
        public const string RecipeIdKey = "r";
        public const string SubRecipeIdKey = "i";
        public const string PayByCrystalKey = "p";
        public const string UseHammerPointKey = "h";
        public const string PetIdKey = "pid";

        public const int BasicSubRecipeHammerPoint = 1;
        public const int SpecialSubRecipeHammerPoint = 2;

        public static void AddAndUnlockOption(
            AgentState agentState,
            PetState petState,
            Equipment equipment,
            IRandom random,
            EquipmentItemSubRecipeSheetV2.Row subRecipe,
            EquipmentItemOptionSheet optionSheet,
            PetOptionSheet petOptionSheet,
            SkillSheet skillSheet
        )
        {
            foreach (var optionInfo in subRecipe.Options
                .OrderByDescending(e => e.Ratio)
                .ThenBy(e => e.RequiredBlockIndex)
                .ThenBy(e => e.Id))
            {
                if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                {
                    continue;
                }

                var value = random.Next(1, GameConfig.MaximumProbability + 1);
                var ratio = optionInfo.Ratio;

                // Apply pet bonus if possible
                if (!(petState is null))
                {
                    ratio = PetHelper.GetBonusOptionProbability(
                        ratio,
                        petState,
                        petOptionSheet);
                }

                if (value > ratio)
                {
                    continue;
                }

                if (optionRow.StatType != StatType.NONE)
                {
                    var stat = CombinationEquipment5.GetStat(optionRow, random);
                    equipment.StatsMap.AddStatAdditionalValue(stat.StatType, stat.BaseValue);
                    equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                    equipment.optionCountFromCombination++;
                    agentState.unlockedOptions.Add(optionRow.Id);
                }
                else
                {
                    var skill = CombinationEquipment16.GetSkill(optionRow, skillSheet, random);
                    if (!(skill is null))
                    {
                        equipment.Skills.Add(skill);
                        equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                        equipment.optionCountFromCombination++;
                        agentState.unlockedOptions.Add(optionRow.Id);
                    }
                }
            }
        }

        public static Skill GetSkill(
            EquipmentItemOptionSheet.Row row,
            SkillSheet skillSheet,
            IRandom random)
        {
            var skillRow = skillSheet.OrderedList.FirstOrDefault(r => r.Id == row.SkillId);
            if (skillRow == null)
            {
                return null;
            }

            var dmg = random.Next(row.SkillDamageMin, row.SkillDamageMax + 1);
            var chance = random.Next(row.SkillChanceMin, row.SkillChanceMax + 1);

            var hasStatDamageRatio = row.StatDamageRatioMin != default && row.StatDamageRatioMax != default;
            var statDamageRatio = hasStatDamageRatio ?
                random.Next(row.StatDamageRatioMin, row.StatDamageRatioMax + 1) : default;
            var refStatType = hasStatDamageRatio ? row.ReferencedStatType : StatType.NONE;

            var skill = SkillFactory.Get(skillRow, dmg, chance, statDamageRatio, refStatType);
            return skill;
        }

        public static void AddSkillOption(
            AgentState agentState,
            Equipment equipment,
            IRandom random,
            EquipmentItemSubRecipeSheetV2.Row subRecipe,
            EquipmentItemOptionSheet optionSheet,
            SkillSheet skillSheet
        )
        {
            foreach (var optionInfo in subRecipe.Options)
            {
                if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                {
                    continue;
                }

                var skill = GetSkill(optionRow, skillSheet, random);
                if (!(skill is null))
                {
                    equipment.Skills.Add(skill);
                    equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                    equipment.optionCountFromCombination++;
                    agentState.unlockedOptions.Add(optionRow.Id);
                }
            }
        }
    }
}
