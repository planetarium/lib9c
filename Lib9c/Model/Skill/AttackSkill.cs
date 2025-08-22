using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.Helper;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public abstract class AttackSkill : Skill
    {
        protected AttackSkill(
            SkillSheet.Row skillRow,
            long power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType) : base(skillRow, power, chance, statPowerRatio, referencedStatType)
        {
        }

        /// <summary>
        /// todo: 캐릭터 스탯에 반영된 버프 효과가 스킬의 순수 데미지에는 영향을 주지 않는 로직.
        /// todo: 타겟의 회피 여부와 상관없이 버프의 확률로 발동되고 있음. 고민이 필요함.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="simulatorWaveTurn"></param>
        /// <param name="isNormalAttack"></param>
        /// <param name="copyCharacter"></param>
        /// <returns></returns>
        protected IEnumerable<BattleStatus.Skill.SkillInfo> ProcessDamage(
            CharacterBase caster,
            int simulatorWaveTurn,
            bool isNormalAttack = false,
            bool copyCharacter = true)
        {
            var infos = new List<BattleStatus.Skill.SkillInfo>();
            var targets = SkillRow.SkillTargetType.GetTarget(caster).ToList();
            var elementalType = SkillRow.ElementalType;

            // Apply stat power ratio
            var powerMultiplier = StatPowerRatio / 10000m;
            var statAdditionalPower = ReferencedStatType != StatType.NONE ?
                NumberConversionHelper.SafeDecimalToInt32(caster.Stats.GetStat(ReferencedStatType) * powerMultiplier) : default;

            long totalDamage = caster.ATK + Power + statAdditionalPower;
            var multipliers = GetMultiplier(SkillRow.HitCount, 1m);
            for (var i = 0; i < SkillRow.HitCount; i++)
            {
                var multiplier = multipliers[i];

                foreach (var target in targets)
                {
                    if (SkillRow.SkillCategory is SkillCategory.ShatterStrike)
                    {
                        totalDamage = NumberConversionHelper.SafeDecimalToInt64(target.HP * powerMultiplier);
                    }

                    long damage = 0;
                    var isCritical = false;
                    // Skill or when normal attack hit.
                    if (!isNormalAttack ||
                        target.IsHit(caster))
                    {
                        // Apply armor penetration and DEF.
                        var finalDEF = DamageHelper.GetFinalDefense(target.DEF, caster.ArmorPenetration);
                        damage = totalDamage - finalDEF;
                        // Apply multiple hits
                        damage = NumberConversionHelper.SafeDecimalToInt64(damage * multiplier);
                        // Apply damage reduction
                        damage = DamageHelper.GetReducedDamage(damage, target.DRV, target.DRR);

                        if (damage < 1)
                        {
                            damage = 1;
                        }
                        else
                        {
                            // 모션 배율 적용.
                            damage = caster.GetDamage(
                                damage,
                                isNormalAttack || SkillRow.Combo
                            );
                            // 속성 적용.
                            damage = elementalType.GetDamage(target.defElementType, damage);
                            // 치명 적용.
                            isCritical =
                                SkillRow.SkillCategory is not SkillCategory.ShatterStrike &&
                                caster.IsCritical(isNormalAttack || SkillRow.Combo);
                            if (isCritical)
                            {
                                damage = CriticalHelper.GetCriticalDamage(caster, damage);
                            }

                            // double attack must be shown as critical attack
                            isCritical |= SkillRow.SkillCategory is SkillCategory.DoubleAttack;

                            // ShatterStrike has max damage limitation
                            if (SkillRow.SkillCategory is SkillCategory.ShatterStrike)
                            {
                                damage = Math.Clamp(damage,
                                    1, caster.Simulator.ShatterStrikeMaxDamage);
                            }
                        }

                        target.CurrentHP -= damage;
                    }

                    var iceShield = target.Buffs.Values.OfType<IceShield>().FirstOrDefault();
                    var clone = copyCharacter ? (CharacterBase) target.Clone() : null;
                    infos.Add(new BattleStatus.Skill.SkillInfo(target.Id, target.IsDead, target.Thorn, damage, isCritical,
                        SkillRow.SkillCategory, simulatorWaveTurn, SkillRow.ElementalType,
                        SkillRow.SkillTargetType, target: clone, iceShield: iceShield));
                }
            }

            return infos;
        }

        protected static decimal[] GetMultiplier(int hitCount, decimal totalDamage)
        {
            if (hitCount == 1) return new[] {totalDamage};
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
