using System.Collections.Generic;

namespace Nekoyume.Model.Skill
{
    public interface IStageSkill
    {
        Model.BattleStatus.Skill Use(
            StageCharacter caster,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs);
    }
}
