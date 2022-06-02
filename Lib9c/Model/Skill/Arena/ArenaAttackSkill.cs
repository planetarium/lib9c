using System;
using System.Collections.Generic;
using Nekoyume.Model.Character;
using Nekoyume.Model.Elemental;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Arena
{
    [Serializable]
    public abstract class ArenaAttackSkill : Skill
    {
        protected ArenaAttackSkill(SkillSheet.Row skillRow, int power, int chance)
            : base(skillRow, power, chance)
        {
        }

         protected IEnumerable<BattleStatus.Skill.SkillInfo> ProcessDamage(
            ArenaPlayer caster,
            ArenaPlayer target,
            int simulatorWaveTurn,
            bool isNormalAttack = false)
        {
            var infos = new List<BattleStatus.Skill.SkillInfo>();

            var multipliers = GetMultiplier(SkillRow.HitCount, 1m);
            var totalDamage = caster.ATK + Power;
            var elementalType = isNormalAttack ? caster.OffensiveElementalType : SkillRow.ElementalType;
            for (var i = 0; i < SkillRow.HitCount; i++)
            {
                var multiplier = multipliers[i];
                var damage = 0;
                var isCritical = false;

                if (target.IsHit(caster))
                {
                    // 방깎 적용.
                    damage = totalDamage - target.DEF;
                    // 멀티 히트 적용.
                    damage = (int) (damage * multiplier);
                    if (damage < 1)
                    {
                        damage = 1;
                    }
                    else
                    {
                        // 모션 배율 적용.
                        damage = caster.GetDamage(damage, isNormalAttack);
                        // 속성 적용.
                        damage = elementalType.GetDamage(target.DefenseElementalType, damage);
                        // 치명 적용.
                        isCritical = caster.IsCritical(isNormalAttack);
                        if (isCritical)
                        {
                            damage = (int) (damage * StageCharacter.CriticalMultiplier);
                        }

                        // 연타공격은 항상 연출이 크리티컬로 보이도록 처리.
                        isCritical |= SkillRow.SkillCategory == SkillCategory.DoubleAttack;
                    }

                    target.CurrentHP -= damage;
                }

                infos.Add(new BattleStatus.Skill.SkillInfo(target, damage, isCritical,
                    SkillRow.SkillCategory, simulatorWaveTurn, SkillRow.ElementalType,
                    SkillRow.SkillTargetType));

            }

            return infos;
        }

         private static decimal[] GetMultiplier(int hitCount, decimal totalDamage)
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
