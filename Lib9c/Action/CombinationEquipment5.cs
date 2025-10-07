using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Battle;
using Lib9c.Model.Item;
using Lib9c.Model.Skill;
using Lib9c.Model.Stat;
using Lib9c.TableData.Item;
using Lib9c.TableData.Skill;
using Libplanet.Action;
using Libplanet.Crypto;

namespace Lib9c.Action
{
    public static class CombinationEquipment5
    {
        public static readonly Address BlacksmithAddress = ItemEnhancement9.BlacksmithAddress;

        public static DecimalStat GetStat(EquipmentItemOptionSheet.Row row, IRandom random)
        {
            var value = random.Next(row.StatMin, row.StatMax + 1);
            return new DecimalStat(row.StatType, value);
        }

        public static Skill GetSkill(EquipmentItemOptionSheet.Row row, SkillSheet skillSheet,
            IRandom random)
        {
            try
            {
                var skillRow = skillSheet.OrderedList.First(r => r.Id == row.SkillId);
                var dmg = random.Next(row.SkillDamageMin, row.SkillDamageMax + 1);
                var chance = random.Next(row.SkillChanceMin, row.SkillChanceMax + 1);

                var hasStatDamageRatio = row.StatDamageRatioMin != default && row.StatDamageRatioMax != default;
                var statDamageRatio = hasStatDamageRatio ?
                    random.Next(row.StatDamageRatioMin, row.StatDamageRatioMax + 1) : default;
                var refStatType = hasStatDamageRatio ? row.ReferencedStatType : StatType.NONE;

                var skill = SkillFactory.Get(skillRow, dmg, chance, statDamageRatio, refStatType);
                return skill;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public static HashSet<int> SelectOption(
            EquipmentItemOptionSheet optionSheet,
            SkillSheet skillSheet,
            EquipmentItemSubRecipeSheet.Row subRecipe,
            IRandom random,
            Equipment equipment
        )
        {
            var optionSelector = new WeightedSelector<EquipmentItemOptionSheet.Row>(random);
            var optionIds = new HashSet<int>();

            // Skip sort subRecipe.Options because it had been already sorted in WeightedSelector.Select();
            foreach (var optionInfo in subRecipe.Options)
            {
                if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                {
                    continue;
                }

                optionSelector.Add(optionRow, optionInfo.Ratio);
            }

            IEnumerable<EquipmentItemOptionSheet.Row> optionRows =
                new EquipmentItemOptionSheet.Row[0];
            try
            {
                optionRows = optionSelector.SelectV1(subRecipe.MaxOptionLimit);
            }
            catch (Exception e) when (
                e is InvalidCountException ||
                e is ListEmptyException
            )
            {
                return optionIds;
            }
            finally
            {
                foreach (var optionRow in optionRows.OrderBy(r => r.Id))
                {
                    if (optionRow.StatType != StatType.NONE)
                    {
                        var stat = GetStat(optionRow, random);
                        equipment.StatsMap.AddStatAdditionalValue(stat.StatType, stat.BaseValue);
                    }
                    else
                    {
                        var skill = GetSkill(optionRow, skillSheet, random);
                        if (!(skill is null))
                        {
                            equipment.Skills.Add(skill);
                        }
                    }

                    optionIds.Add(optionRow.Id);
                }
            }

            return optionIds;
        }
    }
}
