using System;
using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Stage
{
    [Serializable]
    public class NormalAttack : AttackSkill, IStageSkill
    {
        public NormalAttack(SkillSheet.Row skillRow, int power, int chance)
            : base(skillRow, power, chance)
        {
        }

        public Model.BattleStatus.Skill Use(
            StageCharacter caster,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (StageCharacter)caster.Clone();
            var damage = ProcessDamage(caster, simulatorWaveTurn, true);
            var buff = ProcessBuff(caster, simulatorWaveTurn, buffs);

            return new Model.BattleStatus.NormalAttack(clone, damage, buff);
        }
    }
}
