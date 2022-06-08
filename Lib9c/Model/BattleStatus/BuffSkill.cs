using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.Character;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class Buff : Skill
    {
        public Buff(ICharacter character, IEnumerable<SkillInfo> skillInfos)
            : base(character, skillInfos, null)
        {
        }

        public override IEnumerator CoExecute(IWorld world)
        {
            yield return world.CoBuff(Character, SkillInfos, BuffInfos);
        }
    }
}
