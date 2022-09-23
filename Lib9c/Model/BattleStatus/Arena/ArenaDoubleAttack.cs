using System;
using System.Collections;
using System.Collections.Generic;

#nullable disable
namespace Nekoyume.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaDoubleAttack : ArenaSkill
    {
        public ArenaDoubleAttack(
            ArenaCharacter character,
            IEnumerable<ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkillInfo> buffInfos)
            : base(character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoDoubleAttack(Character, SkillInfos, BuffInfos);
        }
    }
}
