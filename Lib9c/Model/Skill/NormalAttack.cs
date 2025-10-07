using System;
using System.Collections.Generic;
using Lib9c.Model.Character;
using Lib9c.Model.Stat;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Skill
{
    [Serializable]
    public class NormalAttack : AttackSkill
    {
        public NormalAttack(
            SkillSheet.Row skillRow,
            long power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType) : base(skillRow, power, chance, statPowerRatio, referencedStatType)
        {
        }

        public override BattleStatus.Skill Use(CharacterBase caster,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs, bool copyCharacter)
        {
            var clone = copyCharacter ? (CharacterBase) caster.Clone() : null;
            var damage = ProcessDamage(caster, simulatorWaveTurn, true, copyCharacter);
            var buff = ProcessBuff(caster, simulatorWaveTurn, buffs, copyCharacter);

            return new Model.BattleStatus.NormalAttack(SkillRow.Id, clone, damage, buff);
        }
    }
}
