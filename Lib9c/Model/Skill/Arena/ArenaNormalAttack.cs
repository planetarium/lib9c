using System;
using System.Collections.Generic;
using Nekoyume.TableData;
using Nekoyume.Model.Character;

namespace Nekoyume.Model.Skill.Arena
{
    [Serializable]
    public class ArenaNormalAttack : ArenaAttackSkill, IArenaSkill
    {
        public ArenaNormalAttack(SkillSheet.Row skillRow, int power, int chance)
            : base(skillRow, power, chance)
        {
        }

        public BattleStatus.Skill Use(
            ArenaPlayer caster,
            ArenaPlayer target,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs)
        {
            var damage = ProcessDamage(caster, target, simulatorWaveTurn, true);
            var buff = ProcessBuffForArena(target, simulatorWaveTurn, buffs);

            return new BattleStatus.NormalAttack(caster, damage, buff);
        }
    }
}
