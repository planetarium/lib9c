using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.Character;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class HealSkill : Skill
    {
        public HealSkill(
            ICharacter character,
            IEnumerable<SkillInfo> skillInfos,
            IEnumerable<SkillInfo> buffInfos)
            : base(character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IWorld world)
        {
            yield return world.CoHeal(Character, SkillInfos, BuffInfos);
        }
    }
}
