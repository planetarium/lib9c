using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class ShatterStrike : Skill
    {
        public ShatterStrike(int skillId, CharacterBase character,
            IEnumerable<SkillInfo> skillInfos, IEnumerable<SkillInfo> buffInfos)
            : base(skillId, character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoShatterStrike(Character, SkillId, SkillInfos, BuffInfos);
        }
    }
}
