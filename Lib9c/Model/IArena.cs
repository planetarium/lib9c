using System.Collections;
using System.Collections.Generic;
using Lib9c.Model.BattleStatus.Arena;
using Lib9c.Model.Character;

namespace Lib9c.Model
{
    public interface IArena
    {
        IEnumerator CoSpawnCharacter(ArenaCharacter character);

        IEnumerator CoNormalAttack(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoBlowAttack(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoDoubleAttack(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoDoubleAttackWithCombo(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoAreaAttack(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoBuffRemovalAttack(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoShatterStrike(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoHeal(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoBuff(ArenaCharacter caster, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos, IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos);
        IEnumerator CoTickDamage(ArenaCharacter affectedCharacter, IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos);
        IEnumerator CoRemoveBuffs(ArenaCharacter caster);
        IEnumerator CoDead(ArenaCharacter caster);
        IEnumerator CoTurnEnd(int turnNumber);
        IEnumerator CoCustomEvent(ArenaCharacter caster, ArenaEventBase eventBase);
    }
}
