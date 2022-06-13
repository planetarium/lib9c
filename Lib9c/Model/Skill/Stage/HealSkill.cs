using System;
using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Stage
{
    [Serializable]
    public class HealSkill : Skill, IStageSkill
    {
        public HealSkill(SkillSheet.Row skillRow, int power, int chance)
            : base(skillRow, power, chance)
        {
        }

        public BattleStatus.Skill Use(
            StageCharacter caster,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (StageCharacter) caster.Clone();
            var heal = ProcessHeal(caster, simulatorWaveTurn);
            var buff = ProcessBuff(caster, simulatorWaveTurn, buffs);

            return new BattleStatus.HealSkill(clone, heal, buff);
        }

        protected IEnumerable<BattleStatus.Skill.SkillInfo> ProcessHeal(
            StageCharacter caster,
            int simulatorWaveTurn)
        {
            var infos = new List<BattleStatus.Skill.SkillInfo>();
            var healPoint = caster.ATK + Power;
            foreach (var target in SkillRow.SkillTargetType.GetTarget(caster))
            {
                target.Heal(healPoint);
                infos.Add(new BattleStatus.Skill.SkillInfo(
                    (StageCharacter)target.Clone(),
                    healPoint,
                    caster.IsCritical(false),
                    SkillRow.SkillCategory,
                    simulatorWaveTurn));
            }

            return infos;
        }
    }
}
