using System;
using System.Collections;
using System.Collections.Generic;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus
{
    [Serializable]
    public class BlowAttack : Skill
    {
        public BlowAttack(int skillId, CharacterBase character, IEnumerable<SkillInfo> skillInfos, IEnumerable<SkillInfo> buffInfos)
            : base(skillId, character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoBlowAttack(Character, SkillId, SkillInfos, BuffInfos);
        }
    }
}
