using System.Collections.Generic;
using Nekoyume.Model.Character;

namespace Nekoyume.Model.Skill
{
    public interface IArenaSkill
    {
        BattleStatus.Skill Use(
            ArenaPlayer caster,
            ArenaPlayer target,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs);
    }
}
