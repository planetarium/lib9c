using Lib9c.TableData.Character;
using Lib9c.TableData.Item;
using Lib9c.TableData.Rune;
using Lib9c.TableData.Skill;
using Lib9c.TableData.WorldAndStage;

namespace Lib9c.TableData
{
    public class SimulatorSheets : SimulatorSheetsV1
    {
        public readonly RuneOptionSheet RuneOptionSheet;
        public readonly RuneListSheet RuneListSheet;
        public readonly RuneLevelBonusSheet RuneLevelBonusSheet;

        public SimulatorSheets(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            RuneOptionSheet runeOptionSheet,
            RuneListSheet runeListSheet,
            RuneLevelBonusSheet runeLevelBonusSheet
        ) : base(
            materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet)
        {
            RuneOptionSheet = runeOptionSheet;
            RuneListSheet = runeListSheet;
            RuneLevelBonusSheet = runeLevelBonusSheet;
        }
    }

    public class StageSimulatorSheets : SimulatorSheets
    {
        public readonly StageSheet StageSheet;
        public readonly StageWaveSheet StageWaveSheet;
        public readonly EnemySkillSheet EnemySkillSheet;

        public StageSimulatorSheets(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            StageSheet stageSheet,
            StageWaveSheet stageWaveSheet,
            EnemySkillSheet enemySkillSheet,
            RuneOptionSheet runeOptionSheet,
            RuneListSheet runeListSheet,
            RuneLevelBonusSheet runeLevelBonusSheet
        ) : base(
            materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet,
            runeOptionSheet,
            runeListSheet,
            runeLevelBonusSheet
        )
        {
            StageSheet = stageSheet;
            StageWaveSheet = stageWaveSheet;
            EnemySkillSheet = enemySkillSheet;
        }
    }

    public class RankingSimulatorSheets : SimulatorSheets
    {
        public readonly WeeklyArenaRewardSheet WeeklyArenaRewardSheet;

        public RankingSimulatorSheets(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            WeeklyArenaRewardSheet weeklyArenaRewardSheet,
            RuneOptionSheet runeOptionSheet,
            RuneListSheet runeListSheet,
            RuneLevelBonusSheet runeLevelBonusSheet
        ) : base(
            materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet,
            runeOptionSheet,
            runeListSheet,
            runeLevelBonusSheet
        )
        {
            WeeklyArenaRewardSheet = weeklyArenaRewardSheet;
        }
    }

    public class ArenaSimulatorSheets : SimulatorSheets
    {
        public CostumeStatSheet CostumeStatSheet { get; }
        public WeeklyArenaRewardSheet WeeklyArenaRewardSheet { get; }

        public ArenaSimulatorSheets(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            CostumeStatSheet costumeStatSheet,
            WeeklyArenaRewardSheet weeklyArenaRewardSheet,
            RuneOptionSheet runeOptionSheet,
            RuneListSheet runeListSheet,
            RuneLevelBonusSheet runeLevelBonusSheet
        ) : base(materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet,
            runeOptionSheet,
            runeListSheet,
            runeLevelBonusSheet
            )
        {
            CostumeStatSheet = costumeStatSheet;
            WeeklyArenaRewardSheet = weeklyArenaRewardSheet;

        }
    }

    public class RaidSimulatorSheets : SimulatorSheets
    {
        public WorldBossCharacterSheet WorldBossCharacterSheet { get; }
        public WorldBossActionPatternSheet WorldBossActionPatternSheet { get; }
        public WorldBossBattleRewardSheet WorldBossBattleRewardSheet { get; }
        public RuneWeightSheet RuneWeightSheet { get; }
        public RuneSheet RuneSheet { get; }

        public RaidSimulatorSheets(
            MaterialItemSheet materialItemSheet,
            SkillSheet skillSheet,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            WorldBossCharacterSheet worldBossCharacterSheet,
            WorldBossActionPatternSheet worldBossActionPatternSheet,
            WorldBossBattleRewardSheet worldBossBattleRewardSheet,
            RuneWeightSheet runeWeightSheet,
            RuneSheet runeSheet,
            RuneOptionSheet runeOptionSheet,
            RuneListSheet runeListSheet,
            RuneLevelBonusSheet runeLevelBonusSheet
        ) : base(materialItemSheet,
            skillSheet,
            skillBuffSheet,
            statBuffSheet,
            skillActionBuffSheet,
            actionBuffSheet,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet,
            runeOptionSheet,
            runeListSheet,
            runeLevelBonusSheet
            )
        {
            WorldBossCharacterSheet = worldBossCharacterSheet;
            WorldBossActionPatternSheet = worldBossActionPatternSheet;
            WorldBossBattleRewardSheet = worldBossBattleRewardSheet;
            RuneWeightSheet = runeWeightSheet;
            RuneSheet = runeSheet;
        }
    }
}
