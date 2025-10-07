using System.Collections;
using System.Collections.Generic;
using Lib9c.Model.BattleStatus;
using Lib9c.Model.Character;
using Lib9c.Model.Item;
using Lib9c.TableData.AdventureBoss;

namespace Lib9c.Model
{
    public interface IStage
    {
        IEnumerator CoSpawnPlayer(Player character);
        IEnumerator CoSpawnEnemyPlayer(EnemyPlayer character);

        #region Skill

        IEnumerator CoNormalAttack(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoBlowAttack(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoDoubleAttack(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoDoubleAttackWithCombo(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoAreaAttack(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoBuffRemovalAttack(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoHeal(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoBuff(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoTickDamage(CharacterBase affectedCharacter, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos);
        IEnumerator CoShatterStrike(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        #endregion

        IEnumerator CoRemoveBuffs(CharacterBase caster);

        IEnumerator CoDropBox(List<ItemBase> items);
        IEnumerator CoGetReward(List<ItemBase> rewards);
        IEnumerator CoSpawnWave(int waveNumber, int waveTurn, List<Enemy> enemies, bool hasBoss);
        IEnumerator CoGetExp(long exp);
        IEnumerator CoWaveTurnEnd(int turnNumber, int waveTurn);
        IEnumerator CoDead(CharacterBase character);
        IEnumerator CoCustomEvent(CharacterBase character, EventBase eventBase);

        #region AdvetureBoss
        IEnumerator CoBreakthrough(CharacterBase character, int floorId, List<AdventureBossFloorWaveSheet.MonsterData> monsters);
        IEnumerator CoStageBuff(CharacterBase affected,  int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        #endregion
    }
}
