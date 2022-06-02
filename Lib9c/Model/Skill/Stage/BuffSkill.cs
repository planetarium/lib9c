using System;
using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Stage
{
    [Serializable]
    public class BuffSkill : Skill
    {
        public BuffSkill(SkillSheet.Row skillRow, int power, int chance)
            : base(skillRow, power, chance)
        {
        }

        public BattleStatus.Skill Use(StageCharacter caster, int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (StageCharacter) caster.Clone();
            var buff = ProcessBuff(caster, simulatorWaveTurn, buffs);

            return new BattleStatus.Buff(clone, buff);
        }

        // public BattleStatus.Skill UseForArena(
        //     ArenaPlayer caster,
        //     ArenaPlayer target,
        //     int simulatorWaveTurn,
        //     IEnumerable<Buff.Buff> buffs)
        // {
        //     var buff = ProcessBuffForArena(target, simulatorWaveTurn, buffs);
        //
        //     return new Model.BattleStatus.Buff(target, buff);
        // }
    }
}
