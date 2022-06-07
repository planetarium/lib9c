using System.Collections.Generic;
using Nekoyume.Model.Character;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Arena
{
    public class ArenaBuffSkill : Skill, IArenaSkill
    {
        public ArenaBuffSkill(SkillSheet.Row skillRow, int power, int chance)
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
            var buff = ProcessBuff(caster, target, simulatorWaveTurn, buffs);

            return new Model.BattleStatus.Buff(clone, buff);
        }
    }
}
