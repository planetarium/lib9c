using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.Item;

namespace Nekoyume.Model
{
    public interface IWorld
    {
        IEnumerator CoNormalAttack(ICharacter caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoBlowAttack(ICharacter caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoDoubleAttack(ICharacter caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoAreaAttack(ICharacter caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoHeal(ICharacter caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoBuff(ICharacter caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoRemoveBuffs(ICharacter caster);
        IEnumerator CoDead(ICharacter caster);
    }
}
