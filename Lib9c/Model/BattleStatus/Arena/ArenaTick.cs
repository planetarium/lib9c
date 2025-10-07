using System;
using System.Collections;
using System.Collections.Generic;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaTick : ArenaSkill
    {
        public ArenaTick(ArenaCharacter character) : this(
            0,
            character,
            ArraySegment<ArenaSkillInfo>.Empty,
            ArraySegment<ArenaSkillInfo>.Empty)
        {
        }

        public ArenaTick(int skillId, ArenaCharacter character, IEnumerable<ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkillInfo> buffInfos)
            : base(skillId, character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoCustomEvent(Character, this);
        }
    }
}
