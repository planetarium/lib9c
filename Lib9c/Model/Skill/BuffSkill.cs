using System;
using System.Collections.Generic;
using Lib9c.Model.Character;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Skill
{
    [Serializable]
    public class BuffSkill : Skill
    {
        public BuffSkill(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override BattleStatus.Skill Use(CharacterBase caster, int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (CharacterBase) caster.Clone();
            var buff = ProcessBuff(caster, simulatorWaveTurn, buffs);

            return new BattleStatus.Buff(SkillRow.Id, clone, buff);
        }
    }
}
