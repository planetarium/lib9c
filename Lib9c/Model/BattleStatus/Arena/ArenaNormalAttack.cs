using System;
using System.Collections;
using System.Collections.Generic;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaNormalAttack : ArenaSkill
    {
        public ArenaNormalAttack(
            ArenaCharacter character,
            IEnumerable<ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkillInfo> buffInfos)
            : base(character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoNormalAttack(Character, SkillInfos, BuffInfos);
        }
    }
}
