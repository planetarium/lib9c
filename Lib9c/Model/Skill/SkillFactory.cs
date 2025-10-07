using System;
using System.Linq;
using Bencodex.Types;
using Lib9c.Battle;
using Lib9c.Model.Item;
using Lib9c.Model.Skill.Arena;
using Lib9c.Model.Stat;
using Lib9c.Model.State;
using Lib9c.TableData.CustomEquipmentCraft;
using Lib9c.TableData.Item;
using Lib9c.TableData.Skill;
using Libplanet.Action;

namespace Lib9c.Model.Skill
{
    public static class SkillFactory
    {
        public static Skill Get(
            SkillSheet.Row skillRow,
            long power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType)
        {
            switch (skillRow.SkillType)
            {
                case SkillType.Attack:
                    switch (skillRow.SkillCategory)
                    {
                        case SkillCategory.NormalAttack:
                            return new NormalAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.DoubleAttack:
                            return new DoubleAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.BlowAttack:
                            return new BlowAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.AreaAttack:
                            return new AreaAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.BuffRemovalAttack:
                            return new BuffRemovalAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.ShatterStrike:
                            return new ShatterStrike(skillRow, power, chance, statPowerRatio,
                                referencedStatType);
                        default:
                            return new NormalAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                    }
                case SkillType.Heal:
                    return new HealSkill(skillRow, power, chance, statPowerRatio, referencedStatType);
                case SkillType.Buff:
                case SkillType.Debuff:
                    return new BuffSkill(skillRow, power, chance, statPowerRatio, referencedStatType);
            }

            throw new UnexpectedOperationException(
                $"{skillRow.Id}, {skillRow.SkillType}, {skillRow.SkillTargetType}, {skillRow.SkillCategory}");
        }

        [Obsolete("Use Get() instead.")]
        public static Skill GetV1(
            SkillSheet.Row skillRow,
            long power,
            int chance)
        {
            switch (skillRow.SkillType)
            {
                case SkillType.Attack:
                    switch (skillRow.SkillCategory)
                    {
                        case SkillCategory.NormalAttack:
                            return new NormalAttack(skillRow, power, chance, default, StatType.NONE);
                        case SkillCategory.DoubleAttack:
                            return new DoubleAttack(skillRow, power, chance, default, StatType.NONE);
                        case SkillCategory.BlowAttack:
                            return new BlowAttack(skillRow, power, chance, default, StatType.NONE);
                        case SkillCategory.AreaAttack:
                            return new AreaAttack(skillRow, power, chance, default, StatType.NONE);
                        case SkillCategory.BuffRemovalAttack:
                            return new BuffRemovalAttack(skillRow, power, chance, default, StatType.NONE);
                        default:
                            return new NormalAttack(skillRow, power, chance, default, StatType.NONE);
                    }
                case SkillType.Heal:
                    return new HealSkill(skillRow, power, chance, default, StatType.NONE);
                case SkillType.Buff:
                case SkillType.Debuff:
                    return new BuffSkill(skillRow, power, chance, default, StatType.NONE);
            }

            throw new UnexpectedOperationException(
                $"{skillRow.Id}, {skillRow.SkillType}, {skillRow.SkillTargetType}, {skillRow.SkillCategory}");
        }

        // Convert skill to arena skill
        public static ArenaSkill GetForArena(
            SkillSheet.Row skillRow,
            long power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType)
        {
            switch (skillRow.SkillType)
            {
                case SkillType.Attack:
                    switch (skillRow.SkillCategory)
                    {
                        case SkillCategory.NormalAttack:
                            return new ArenaNormalAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.DoubleAttack:
                            return new ArenaDoubleAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.BlowAttack:
                            return new ArenaBlowAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.AreaAttack:
                            return new ArenaAreaAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        case SkillCategory.BuffRemovalAttack:
                            return new ArenaBuffRemovalAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                        default:
                            return new ArenaNormalAttack(skillRow, power, chance, statPowerRatio, referencedStatType);
                    }
                case SkillType.Heal:
                    return new ArenaHealSkill(skillRow, power, chance, statPowerRatio, referencedStatType);
                case SkillType.Buff:
                case SkillType.Debuff:
                    return new ArenaBuffSkill(skillRow, power, chance, statPowerRatio, referencedStatType);
            }

            throw new UnexpectedOperationException(
                $"{skillRow.Id}, {skillRow.SkillType}, {skillRow.SkillTargetType}, {skillRow.SkillCategory}");
        }

        /// <summary>
        /// Deserializes a skill from serialized data.
        /// Supports both Dictionary and List formats for backward compatibility.
        /// </summary>
        /// <param name="serialized">The serialized skill data</param>
        /// <returns>The deserialized skill</returns>
        /// <exception cref="ArgumentException">Thrown when the serialization format is not supported</exception>
        /// <remarks>
        /// <para>
        /// This method automatically detects the serialization format and delegates to the appropriate
        /// deserialization method. It supports both the new List-based format and the legacy Dictionary format
        /// to ensure backward compatibility with existing data.
        /// </para>
        /// <para>
        /// List format (new): [SkillRow, Power, Chance, StatPowerRatio?, ReferencedStatType?]
        /// Dictionary format (legacy): {"skillRow": ..., "power": ..., "chance": ..., "stat_power_ratio": ..., "referenced_stat_type": ...}
        /// </para>
        /// </remarks>
        public static Skill Deserialize(IValue serialized)
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
        /// Deserializes a skill from the new List-based serialization format.
        /// </summary>
        /// <param name="serialized">The serialized skill data in List format</param>
        /// <returns>The deserialized skill</returns>
        /// <remarks>
        /// <para>
        /// Expected List format: [SkillRow, Power, Chance, StatPowerRatio?, ReferencedStatType?]
        /// Where StatPowerRatio and ReferencedStatType are optional and only included if StatPowerRatio > 0.
        /// </para>
        /// <para>
        /// This method handles the new List-based serialization format which is more compact
        /// and performant than the previous Dictionary format.
        /// </para>
        /// </remarks>
        public static Skill DeserializeFromList(List serialized)
        {
            var skillRow = SkillSheet.Row.Deserialize(serialized[0]);
            var power = serialized[1].ToInteger();
            var chance = serialized[2].ToInteger();

            var ratio = 0;
            var statType = StatType.NONE;

            if (serialized.Count > 3)
            {
                ratio = serialized[3].ToInteger();
                statType = StatTypeExtension.Deserialize((Binary)serialized[4]);
            }

            return Get(skillRow, power, chance, ratio, statType);
        }

        /// <summary>
        /// Deserializes a skill from the legacy Dictionary-based serialization format.
        /// This method is maintained for backward compatibility with existing data.
        /// </summary>
        /// <param name="serialized">The serialized skill data in Dictionary format</param>
        /// <returns>The deserialized skill</returns>
        /// <remarks>
        /// <para>
        /// Expected Dictionary format:
        /// {
        ///   "skillRow": SkillSheet.Row,
        ///   "power": long,
        ///   "chance": int,
        ///   "stat_power_ratio": int (optional),
        ///   "referenced_stat_type": StatType (optional)
        /// }
        /// </para>
        /// <para>
        /// This method is marked as obsolete and should only be used for backward compatibility.
        /// New code should use the List-based format through Deserialize(IValue) or DeserializeFromList(List).
        /// </para>
        /// </remarks>
        [Obsolete("Use Deserialize(IValue) instead.")]
        public static Skill DeserializeFromDictionary(Dictionary serialized)
        {
            var ratio = serialized.TryGetValue((Text)"stat_power_ratio", out var ratioValue) ?
                ratioValue.ToInteger() : default;
            var statType = serialized.TryGetValue((Text)"referenced_stat_type", out var refStatType) ?
                StatTypeExtension.Deserialize((Binary)refStatType) : StatType.NONE;

            return Get(
                SkillSheet.Row.Deserialize(serialized["skillRow"]),
                serialized["power"].ToInteger(),
                serialized["chance"].ToInteger(),
                ratio,
                statType
            );
        }

        public static Skill SelectSkill(
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
    }
}
