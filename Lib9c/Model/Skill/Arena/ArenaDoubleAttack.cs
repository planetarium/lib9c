using System;
using System.Collections.Generic;
using Lib9c.Model.Character;
using Lib9c.Model.Stat;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Skill.Arena
{
    [Serializable]
    public class ArenaDoubleAttack : ArenaAttackSkill
    {
        public ArenaDoubleAttack(
            SkillSheet.Row skillRow,
            long power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType) : base(skillRow, power, chance, statPowerRatio,
            referencedStatType)
        {
        }

        public override BattleStatus.Arena.ArenaSkill Use(
            ArenaCharacter caster,
            ArenaCharacter target,
            int turn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (ArenaCharacter) caster.Clone();
            var damage = ProcessDamage(caster, target, turn);
            var buff = ProcessBuff(caster, target, turn, buffs);

            if (SkillRow.Combo)
            {
                return new BattleStatus.Arena.ArenaDoubleAttackWithCombo(SkillRow.Id, clone, damage, buff);
            }
            else
            {
                return new BattleStatus.Arena.ArenaDoubleAttack(SkillRow.Id, clone, damage, buff);
            }
        }
    }
}
