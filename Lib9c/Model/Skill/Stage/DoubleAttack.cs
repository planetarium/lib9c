using System;
using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Stage
{
    [Serializable]
    public class DoubleAttack : AttackSkill, IStageSkill
    {
        public DoubleAttack(SkillSheet.Row skillRow, int power, int chance)
            : base(skillRow, power, chance)
        {
        }

        public BattleStatus.Skill Use(
            StageCharacter caster,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (StageCharacter) caster.Clone();
            var damage = ProcessDamage(caster, simulatorWaveTurn);
            var buff = ProcessBuff(caster, simulatorWaveTurn, buffs);

            return new Model.BattleStatus.DoubleAttack(clone, damage, buff);
        }


    }
}
