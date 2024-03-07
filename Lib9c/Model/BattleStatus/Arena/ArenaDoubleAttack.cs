using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaDoubleAttack : ArenaSkill
    {
        public readonly int skillId;
        public ArenaDoubleAttack(
            ArenaCharacter character,
            IEnumerable<ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkillInfo> buffInfos,
            int skillId)
            : base(character, skillInfos, buffInfos)
        {
            this.skillId = skillId;
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoDoubleAttack(Character, SkillInfos, BuffInfos, skillId);
        }
    }
}
