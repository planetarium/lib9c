using System;
using System.Collections;
using System.Collections.Generic;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus
{
    [Serializable]
    public class Buff : Skill
    {
        public Buff(int skillId, CharacterBase character, IEnumerable<SkillInfo> skillInfos)
            : base(skillId, character, skillInfos, null)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoBuff(Character, SkillId, SkillInfos, BuffInfos);
        }
    }
}
