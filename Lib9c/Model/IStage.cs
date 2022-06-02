using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.Character;
using Nekoyume.Model.Item;

namespace Nekoyume.Model
{
    public interface IStage
    {
        IEnumerator CoSpawnPlayer(Player character);
        IEnumerator CoSpawnEnemyPlayer(EnemyPlayer character);

        #region Skill

        IEnumerator CoNormalAttack(ICharacter caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoBlowAttack(ICharacter caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoDoubleAttack(ICharacter caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoAreaAttack(ICharacter caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoHeal(ICharacter caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoBuff(ICharacter caster, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);

        #endregion

        IEnumerator CoRemoveBuffs(ICharacter caster);

        IEnumerator CoDropBox(List<ItemBase> items);
        IEnumerator CoGetReward(List<ItemBase> rewards);
        IEnumerator CoSpawnWave(int waveNumber, int waveTurn, List<Enemy> enemies, bool hasBoss);
        IEnumerator CoGetExp(long exp);
        IEnumerator CoWaveTurnEnd(int turnNumber, int waveTurn);
        IEnumerator CoDead(ICharacter character);


        #region Arena

        IEnumerator CoSpawnArenaPlayer(ArenaPlayer character);
        IEnumerator CoArenaTurnEnd(int turnNumber);

        #endregion
    }
}
