using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Battle;
using Lib9c.Helper;
using Lib9c.Model.Buff;
using Lib9c.Model.Character;
using Lib9c.Model.Elemental;
using Lib9c.Model.Stat;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Skill.Arena
{
    [Serializable]
    public abstract class ArenaAttackSkill : ArenaSkill
    {
        protected ArenaAttackSkill(
            SkillSheet.Row skillRow,
            long power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType) : base(skillRow, power, chance, statPowerRatio, referencedStatType)
        {
        }

        /// <summary>
        /// Processes damage calculation for arena attack skills with improved large value handling.
        /// Uses NumberConversionHelper.SafeDecimalToInt64 to prevent overflow when dealing with large stat values.
        /// Implements DamageHelper.GetFinalDefense and DamageHelper.GetReducedDamage for consistent damage calculation.
        /// </summary>
        /// <param name="caster">The arena character casting the skill</param>
        /// <param name="target">The target arena character</param>
        /// <param name="simulatorWaveTurn">Current wave turn in the arena simulator</param>
        /// <param name="isNormalAttack">Whether this is a normal attack</param>
        /// <returns>Collection of arena skill information including damage and effects</returns>
        protected IEnumerable<BattleStatus.Arena.ArenaSkill.ArenaSkillInfo> ProcessDamage(
            ArenaCharacter caster,
            ArenaCharacter target,
            int simulatorWaveTurn,
            bool isNormalAttack = false)
        {
            var infos = new List<BattleStatus.Arena.ArenaSkill.ArenaSkillInfo>();

            // Apply stat power ratio
            var powerMultiplier = StatPowerRatio / 10000m;
            var statAdditionalPower = ReferencedStatType != StatType.NONE ?
                NumberConversionHelper.SafeDecimalToInt32(caster.Stats.GetStat(ReferencedStatType) * powerMultiplier) : default;

            var multipliers = GetMultiplier(SkillRow.HitCount, 1m);
            var elementalType = isNormalAttack ? caster.OffensiveElementalType : SkillRow.ElementalType;
            for (var i = 0; i < SkillRow.HitCount; i++)
            {
                var multiplier = multipliers[i];
                long damage = 0;
                var isCritical = false;

                if (target.IsHit(caster))
                {
                    var total = SkillRow.SkillCategory is SkillCategory.ShatterStrike
                        ? target.HP * powerMultiplier
                        // Use NumberConversionHelper.SafeDecimalToInt64 to prevent overflow when dealing with large ATK values
                        : caster.Stats.GetStat(StatType.ATK) + Power + statAdditionalPower;
                    damage = NumberConversionHelper.SafeDecimalToInt64(total * multiplier);
                    damage = caster.GetDamage(damage, isNormalAttack || SkillRow.Combo);
                    damage = elementalType.GetDamage(target.DefenseElementalType, damage);
                    isCritical = SkillRow.SkillCategory is not SkillCategory.ShatterStrike &&
                                 caster.IsCritical(isNormalAttack || SkillRow.Combo);
                    if (isCritical)
                    {
                        damage = CriticalHelper.GetCriticalDamageForArena(caster, damage);
                    }

                    // Apply armor penetration and DEF using DamageHelper for consistent calculation
                    var finalDEF = DamageHelper.GetFinalDefense(target.DEF, caster.ArmorPenetration);
                    damage = Math.Max(damage - finalDEF, 1);
                    // Apply damage reduction using DamageHelper for consistent calculation
                    damage = DamageHelper.GetReducedDamage(damage, target.DRV, target.DRR);

                    // ShatterStrike has max damage limitation
                    if (SkillRow.SkillCategory is SkillCategory.ShatterStrike)
                    {
                        damage = Math.Clamp(damage,
                            1, caster.Simulator.ShatterStrikeMaxDamage);
                    }

                    target.CurrentHP -= damage;

                    // double attack must be shown as critical attack
                    isCritical |= SkillRow.SkillCategory == SkillCategory.DoubleAttack;
                }

                var iceShield = target.Buffs.Values.OfType<IceShield>().FirstOrDefault();
                infos.Add(new BattleStatus.Arena.ArenaSkill.ArenaSkillInfo(
                    (ArenaCharacter)target.Clone(),
                    damage,
                    isCritical,
                    SkillRow.SkillCategory,
                    simulatorWaveTurn,
                    elementalType,
                    SkillRow.SkillTargetType,
                    iceShield: iceShield));
            }

            return infos;
        }

        /// <summary>
        /// Calculates damage multipliers for multi-hit arena skills.
        /// For single hit skills, returns the total damage as is.
        /// For multi-hit skills, distributes the total damage across hits with the last hit dealing 30% more damage.
        /// This creates a pattern where the final hit is more impactful than the preceding hits.
        /// </summary>
        /// <param name="hitCount">Number of hits for the arena skill</param>
        /// <param name="totalDamage">Total damage to be distributed across hits</param>
        /// <returns>Array of damage multipliers for each hit</returns>
        protected static decimal[] GetMultiplier(int hitCount, decimal totalDamage)
        {
            if (hitCount == 1) return new[] { totalDamage };
            var multiplier = new List<decimal>();
            var avg = totalDamage / hitCount;
            // Last hit deals 30% more damage than average
            var lastDamage = avg * 1.3m;
            var lastHitIndex = hitCount - 1;
            // Distribute remaining damage evenly among other hits
            var eachDamage = (totalDamage - lastDamage) / lastHitIndex;
            for (var i = 0; i < hitCount; i++)
            {
                var result = i == lastHitIndex ? lastDamage : eachDamage;
                multiplier.Add(result);
            }

            return multiplier.ToArray();
        }
    }
}
