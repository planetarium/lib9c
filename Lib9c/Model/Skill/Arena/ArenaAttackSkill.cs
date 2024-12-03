using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Arena
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
                 (int)(caster.Stats.GetStat(ReferencedStatType) * powerMultiplier) : default;

            var multipliers = GetMultiplier(SkillRow.HitCount, 1m);
            var elementalType = isNormalAttack ? caster.OffensiveElementalType : SkillRow.ElementalType;
            for (var i = 0; i < SkillRow.HitCount; i++)
            {
                var multiplier = multipliers[i];
                long damage = 0;
                var isCritical = false;

                if (target.IsHit(caster))
                {
                    damage = (long)(SkillRow.SkillCategory is SkillCategory.ShatterStrike
                        ? target.HP * powerMultiplier
                        : caster.ATK + Power + statAdditionalPower);
                    damage = (long)(damage * multiplier);
                    damage = caster.GetDamage(damage, isNormalAttack || SkillRow.Combo);
                    damage = elementalType.GetDamage(target.DefenseElementalType, damage);
                    isCritical = SkillRow.SkillCategory is not SkillCategory.ShatterStrike &&
                                 caster.IsCritical(isNormalAttack || SkillRow.Combo);
                    if (isCritical)
                    {
                        damage = CriticalHelper.GetCriticalDamageForArena(caster, damage);
                    }

                    // Apply armor penetration and DEF.
                    var finalDEF = Math.Clamp(target.DEF - caster.ArmorPenetration, 0, long.MaxValue);
                    damage = Math.Max(damage - finalDEF, 1);
                    // Apply damage reduce
                    damage = (int)((damage - target.DRV) * DamageHelper.GetDamageReductionRate(target.DRR));

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

        private static decimal[] GetMultiplier(int hitCount, decimal totalDamage)
        {
            if (hitCount == 1) return new[] { totalDamage };
            var multiplier = new List<decimal>();
            var avg = totalDamage / hitCount;
            var lastDamage = avg * 1.3m;
            var lastHitIndex = hitCount - 1;
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
