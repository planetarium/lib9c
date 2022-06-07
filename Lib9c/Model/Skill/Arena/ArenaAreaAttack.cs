using System;
using System.Collections.Generic;
using Nekoyume.Model.Character;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Arena
{
    [Serializable]
    public class ArenaAreaAttack : ArenaAttackSkill, IArenaSkill
    {
        public ArenaAreaAttack(SkillSheet.Row skillRow, int power, int chance)
            : base(skillRow, power, chance)
        {
        }

        public BattleStatus.Skill Use(
            ArenaCharacter caster,
            ArenaCharacter target,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (ArenaCharacter)caster.Clone();
            var damage = ProcessDamage(caster, target, simulatorWaveTurn);
            var buff = ProcessBuff(caster, target, simulatorWaveTurn, buffs);

            return new BattleStatus.AreaAttack(clone, damage, buff);
        }
    }
}
