using System;
using System.Collections.Generic;
using Lib9c.Model.Character;
using Lib9c.Model.Stat;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Skill.Arena
{
    [Serializable]
    public class ArenaNormalAttack : ArenaAttackSkill
    {
        public ArenaNormalAttack(
            SkillSheet.Row skillRow,
            long power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType) : base(skillRow, power, chance, statPowerRatio, referencedStatType)
        {
        }

        public override BattleStatus.Arena.ArenaSkill Use(
            ArenaCharacter caster,
            ArenaCharacter target,
            int turn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (ArenaCharacter)caster.Clone();
            var damage = ProcessDamage(caster, target, turn, true);
            var buff = ProcessBuff(caster, target, turn, buffs);

            return new BattleStatus.Arena.ArenaNormalAttack(SkillRow.Id, clone, damage, buff);
        }
    }
}
