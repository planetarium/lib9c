using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaTick : ArenaSkill
    {
        public ArenaTick(ArenaCharacter character) : this(
            character,
            ArraySegment<ArenaSkillInfo>.Empty,
            ArraySegment<ArenaSkillInfo>.Empty)
        {
        }

        public ArenaTick(ArenaCharacter character, IEnumerable<ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkillInfo> buffInfos)
            : base(character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoCustomEvent(Character, this);
        }
    }
}
