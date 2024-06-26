using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus.AdventureBoss
{
    [Serializable]
    public class StageBuff : Skill
    {
        public StageBuff(
            int skillId, CharacterBase character,
            IEnumerable<SkillInfo> skillInfos, IEnumerable<SkillInfo> buffInfos
        ) : base(skillId, character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoStageBuff(Character, SkillId, SkillInfos, BuffInfos);
        }
    }
}
