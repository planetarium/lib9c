using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaDoubleAttackWithCombo : ArenaSkill
    {
        public ArenaDoubleAttackWithCombo(
            int skillId,
            ArenaCharacter character,
            IEnumerable<ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkillInfo> buffInfos)
            : base(skillId, character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoDoubleAttackWithCombo(Character, SkillInfos, BuffInfos);
        }
    }
}
