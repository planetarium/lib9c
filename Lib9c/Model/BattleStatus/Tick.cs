using System;
using System.Collections;
using System.Collections.Generic;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus
{
    [Serializable]
    public class Tick : Skill
    {
        public Tick(CharacterBase character) : this(
            0,
            character,
            ArraySegment<SkillInfo>.Empty,
            ArraySegment<SkillInfo>.Empty)
        {
        }

        public Tick(int skillId, CharacterBase character, IEnumerable<SkillInfo> skillInfos, IEnumerable<SkillInfo> buffInfos)
            : base(skillId, character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoCustomEvent(Character, this);
        }
    }
}
