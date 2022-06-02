using System;
using System.Collections.Generic;
using Nekoyume.Model.Character;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Arena
{
    [Serializable]
    public class ArenaDoubleAttack : ArenaAttackSkill, IArenaSkill
    {
        public ArenaDoubleAttack(SkillSheet.Row skillRow, int power, int chance)
            : base(skillRow, power, chance)
        {
        }

        public BattleStatus.Skill Use(
            ArenaPlayer caster,
            ArenaPlayer target,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs)
        {
            var damage = ProcessDamage(caster, target, simulatorWaveTurn);
            var buff = ProcessBuffForArena(target, simulatorWaveTurn, buffs);

            return new Model.BattleStatus.DoubleAttack(caster, damage, buff);
        }


    }
}
