using System;
using System.Collections;
using System.Collections.Generic;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaBuff : ArenaSkill
    {
        public ArenaBuff(int skillId, ArenaCharacter character, IEnumerable<ArenaSkillInfo> skillInfos)
            : base(skillId, character, skillInfos, null)
        {
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoBuff(Character, SkillInfos, BuffInfos);
        }
    }
}
