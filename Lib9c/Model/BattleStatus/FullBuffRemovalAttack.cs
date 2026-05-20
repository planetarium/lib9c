using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus
{
    /// <summary>
    /// Battle status event for the full buff removal attack skill.
    /// Triggers the <see cref="IStage.CoFullBuffRemovalAttack"/> coroutine for stage rendering.
    /// </summary>
    [Serializable]
    public class FullBuffRemovalAttack : Skill
    {
        /// <inheritdoc cref="Skill(int, CharacterBase, IEnumerable{SkillInfo}, IEnumerable{SkillInfo})"/>
        public FullBuffRemovalAttack(int skillId, CharacterBase character, IEnumerable<SkillInfo> skillInfos, IEnumerable<SkillInfo> buffInfos)
            : base(skillId, character, skillInfos, buffInfos)
        {
        }

        /// <inheritdoc/>
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoFullBuffRemovalAttack(Character, SkillId, SkillInfos, BuffInfos);
        }
    }
}
