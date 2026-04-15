using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus.Arena
{
    /// <summary>
    /// Arena battle status event for the full buff removal attack skill.
    /// Triggers the <see cref="IArena.CoFullBuffRemovalAttack"/> coroutine for arena rendering.
    /// </summary>
    [Serializable]
    public class ArenaFullBuffRemovalAttack : ArenaSkill
    {
        /// <inheritdoc cref="ArenaSkill(int, ArenaCharacter, IEnumerable{ArenaSkillInfo}, IEnumerable{ArenaSkillInfo})"/>
        public ArenaFullBuffRemovalAttack(
            int skillId,
            ArenaCharacter character,
            IEnumerable<ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkillInfo> buffInfos)
            : base(skillId, character, skillInfos, buffInfos)
        {
        }

        /// <inheritdoc/>
        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoFullBuffRemovalAttack(Character, SkillInfos, BuffInfos);
        }
    }
}
