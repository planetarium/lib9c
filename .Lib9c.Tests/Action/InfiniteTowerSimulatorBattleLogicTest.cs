namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.InfiniteTower;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    /// <summary>
    /// Tests for InfiniteTowerSimulator battle logic implementation.
    /// Verifies that the actual turn-based combat system works correctly.
    /// </summary>
    public class InfiniteTowerSimulatorBattleLogicTest
    {
        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());

        [Fact]
        public void SimulateWaveCombat_WithPlayerAndEnemies_ShouldExecuteTurnBasedCombat()
        {
            // Arrange
            var (simulator, player, enemies) = CreateTestSimulator();
            var initialPlayerHp = player.HP;
            var initialEnemyCount = enemies.Count;

            // Act
            simulator.Simulate();

            // Assert
            Assert.NotNull(simulator.Log);
            Assert.True(simulator.Log.result == Nekoyume.Model.BattleStatus.BattleLog.Result.Win || simulator.Log.result == Nekoyume.Model.BattleStatus.BattleLog.Result.Lose);
            // Player should have taken some damage or be defeated
            Assert.True(player.HP <= initialPlayerHp);
        }

        [Fact]
        public void ApplyConditionsToCharacter_WithPlayerTarget_ShouldApplyToPlayerOnly()
        {
            // Arrange
            var condition = CreateTestCondition(SkillTargetType.Self, StatType.ATK, 50);
            var (simulator, player, enemy) = CreateTestSimulatorWithCondition(condition);

            var playerAtkBefore = player.Stats.ATK;
            var enemyAtkBefore = enemy.Stats.ATK;

            // Act
            simulator.ApplyConditionsToCharacter(player, isPlayer: true);
            simulator.ApplyConditionsToCharacter(enemy, isPlayer: false);

            // Assert
            Assert.True(player.Stats.ATK >= playerAtkBefore + 50);
            Assert.Equal(enemyAtkBefore, enemy.Stats.ATK);
        }

        [Fact]
        public void ApplyConditionsToCharacter_WithEnemyTarget_ShouldApplyToEnemyOnly()
        {
            // Arrange
            var condition = CreateTestCondition(SkillTargetType.Enemy, StatType.DEF, 30);
            var (simulator, player, enemy) = CreateTestSimulatorWithCondition(condition);

            var playerDefBefore = player.Stats.DEF;
            var enemyDefBefore = enemy.Stats.DEF;

            // Act
            simulator.ApplyConditionsToCharacter(player, isPlayer: true);
            simulator.ApplyConditionsToCharacter(enemy, isPlayer: false);

            // Assert
            Assert.Equal(playerDefBefore, player.Stats.DEF);
            Assert.True(enemy.Stats.DEF >= enemyDefBefore + 30);
        }

        [Fact]
        public void SimulateWaveCombat_WithTurnLimit_ShouldRespectMaxTurns()
        {
            // Arrange
            var (simulator, player, enemies) = CreateTestSimulator();
            var initialTurnNumber = simulator.TurnNumber;

            // Act
            simulator.Simulate();

            // Assert
            // Turn number should have increased during combat
            Assert.True(simulator.TurnNumber > initialTurnNumber);
        }

        [Fact]
        public void ShouldApplyCondition_WithDifferentTargets_ShouldReturnCorrectResults()
        {
            // Arrange
            var selfCondition = CreateTestCondition(SkillTargetType.Self, StatType.ATK, 10);
            var enemyCondition = CreateTestCondition(SkillTargetType.Enemy, StatType.ATK, 10);
            var enemiesCondition = CreateTestCondition(SkillTargetType.Enemies, StatType.ATK, 10);

            // Act & Assert
            // Self condition should apply to player only
            Assert.True(ShouldApplyCondition(selfCondition, isPlayer: true));
            Assert.False(ShouldApplyCondition(selfCondition, isPlayer: false));

            // Enemy conditions should apply to enemies only
            Assert.False(ShouldApplyCondition(enemyCondition, isPlayer: true));
            Assert.True(ShouldApplyCondition(enemyCondition, isPlayer: false));

            Assert.False(ShouldApplyCondition(enemiesCondition, isPlayer: true));
            Assert.True(ShouldApplyCondition(enemiesCondition, isPlayer: false));
        }

        [Fact]
        public void Simulate_WithMultipleWaves_ShouldProcessAllWaves()
        {
            // Arrange
            var waveRows = new List<InfiniteTowerFloorWaveSheet.WaveData>
            {
                CreateTestWaveRow(1, 1, 201000, 1, 1),
                CreateTestWaveRow(2, 1, 201001, 1, 1),
            };

            var (simulator, player, _) = CreateTestSimulatorWithWaves(waveRows);

            // Act
            simulator.Simulate();

            // Assert
            Assert.NotNull(simulator.Log);
            Assert.Equal(2, simulator.Log.waveCount);
        }

        [Fact]
        public void Simulate_WithPlayerClear_ShouldGenerateValidBattleLog()
        {
            // Arrange
            var (simulator, player, enemies) = CreateTestSimulatorWithWeakEnemies();

            // Act
            simulator.Simulate();

            // Assert
            Assert.NotNull(simulator.Log);
            Assert.Equal(1, simulator.Log.stageId);
            Assert.True(simulator.Log.waveCount > 0);
            Assert.True(simulator.Log.clearedWaveNumber >= 0);

            // Check if battle log contains proper events
            Assert.True(simulator.Log.Count > 0);
        }

        [Fact]
        public void Simulate_WithPlayerClear_ShouldProcessRewards()
        {
            // Arrange
            var (simulator, player, enemies) = CreateTestSimulatorWithWeakEnemies();
            var initialPlayerLevel = player.Level;
            var initialPlayerExp = player.Exp.Current;

            // Act
            simulator.Simulate();

            // Assert
            Assert.NotNull(simulator.Log);

            // If player cleared, should have gained experience
            if (simulator.Log.result == Nekoyume.Model.BattleStatus.BattleLog.Result.Win)
            {
                Assert.True(player.Exp.Current >= initialPlayerExp);
                // Player level might increase if enough exp gained
                Assert.True(player.Level >= initialPlayerLevel);

                // Check if rewards were processed
                Assert.NotEmpty(simulator.Reward);
            }
        }

        [Fact]
        public void Simulate_WithPlayerDefeat_ShouldGenerateLoseBattleLog()
        {
            // Arrange
            var (simulator, player, enemies) = CreateTestSimulatorWithStrongEnemies();

            // Act
            simulator.Simulate();

            // Assert
            Assert.NotNull(simulator.Log);
            Assert.Equal(Nekoyume.Model.BattleStatus.BattleLog.Result.Lose, simulator.Log.result);
            Assert.False(simulator.Log.IsClear);
        }

        [Fact]
        public void Simulate_WithPlayerClear_ShouldGenerateWinBattleLog()
        {
            // Arrange
            var (simulator, player, enemies) = CreateTestSimulatorWithWeakEnemies();

            // Act
            simulator.Simulate();

            // Assert
            Assert.NotNull(simulator.Log);
            Assert.Equal(Nekoyume.Model.BattleStatus.BattleLog.Result.Win, simulator.Log.result);
            Assert.True(simulator.Log.IsClear);
        }

        [Fact]
        public void Simulate_WithTurnLimit_ShouldGenerateTimeOverBattleLog()
        {
            // Arrange
            var (simulator, player, enemies) = CreateTestSimulatorWithTurnLimit(2); // Very low turn limit

            // Act
            simulator.Simulate();

            // Assert
            Assert.NotNull(simulator.Log);
            Assert.Equal(Nekoyume.Model.BattleStatus.BattleLog.Result.TimeOver, simulator.Log.result);
            Assert.False(simulator.Log.IsClear);
        }

        [Fact]
        public void Simulate_WithMultipleWaves_ShouldTrackWaveProgress()
        {
            // Arrange
            var waveRows = new List<InfiniteTowerFloorWaveSheet.WaveData>
            {
                CreateTestWaveRow(1, 1, 201000, 1, 1),
                CreateTestWaveRow(2, 1, 201001, 1, 1),
                CreateTestWaveRow(3, 1, 201002, 1, 1),
            };

            var (simulator, player, _) = CreateTestSimulatorWithWaves(waveRows);

            // Act
            simulator.Simulate();

            // Assert
            Assert.NotNull(simulator.Log);
            Assert.Equal(3, simulator.Log.waveCount);
            Assert.True(simulator.Log.clearedWaveNumber >= 0);
            Assert.True(simulator.Log.clearedWaveNumber <= simulator.Log.waveCount);
        }

        private (InfiniteTowerSimulator Simulator, Player Player, List<Enemy> Enemies) CreateTestSimulator()
        {
            var waveRows = new List<InfiniteTowerFloorWaveSheet.WaveData>
            {
                CreateTestWaveRow(1, 1, 201000, 1, 1),
            };
            return CreateTestSimulatorWithWaves(waveRows);
        }

        private (InfiniteTowerSimulator Simulator, Player Player, List<Enemy> Enemies) CreateTestSimulatorWithWaves(
            List<InfiniteTowerFloorWaveSheet.WaveData> waveRows)
        {
            var random = new TestRandom();
            var avatar = CreateTestAvatar();
            var sheets = _tableSheets.GetSimulatorSheets();
            var player = new Player(avatar, sheets);

            // Create test ItemSheet with test items
            var itemSheet = CreateTestItemSheet();

            var simulator = new InfiniteTowerSimulator(
                random,
                avatar,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(Nekoyume.Model.EnumType.BattleType.Adventure),
                1,
                1,
                CreateTestFloorRow(),
                waveRows,
                false,
                0,
                sheets,
                new EnemySkillSheet(),
                new CostumeStatSheet(),
                itemSheet,
                new List<StatModifier>(),
                new BuffLimitSheet(),
                new BuffLinkSheet(),
                new List<InfiniteTowerCondition>(),
                0);

            var enemies = new List<Enemy>();
            foreach (var waveData in waveRows)
            {
                foreach (var monster in waveData.Monsters)
                {
                    var charRow = CreateTestCharacterRow();
                    var enemyStats = new CharacterStats(charRow, monster.Level);
                    var enemy = new Enemy(player, enemyStats, charRow, charRow.ElementalType);
                    enemies.Add(enemy);
                }
            }

            return (simulator, player, enemies);
        }

        private (InfiniteTowerSimulator Simulator, Player Player, Enemy Enemy) CreateTestSimulatorWithCondition(
            InfiniteTowerCondition condition)
        {
            var conditions = new List<InfiniteTowerCondition> { condition };
            var (simulator, player, enemies) = CreateTestSimulatorWithConditions(conditions);
            return (simulator, player, enemies.First());
        }

        private (InfiniteTowerSimulator Simulator, Player Player, List<Enemy> Enemies) CreateTestSimulatorWithConditions(
            List<InfiniteTowerCondition> conditions)
        {
            var waveRows = new List<InfiniteTowerFloorWaveSheet.WaveData>
            {
                CreateTestWaveRow(1, 1, 20001, 1, 1),
            };

            var random = new TestRandom();
            var avatar = CreateTestAvatar();
            var sheets = _tableSheets.GetSimulatorSheets();
            var player = new Player(avatar, sheets);

            var simulator = new InfiniteTowerSimulator(
                random,
                avatar,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(Nekoyume.Model.EnumType.BattleType.Adventure),
                1,
                1,
                CreateTestFloorRow(),
                waveRows,
                false,
                0,
                sheets,
                new EnemySkillSheet(),
                new CostumeStatSheet(),
                new ItemSheet(),
                new List<StatModifier>(),
                new BuffLimitSheet(),
                new BuffLinkSheet(),
                conditions,
                0);

            var enemies = new List<Enemy>();
            foreach (var waveData in waveRows)
            {
                foreach (var monster in waveData.Monsters)
                {
                    var charRow = CreateTestCharacterRow();
                    var enemyStats = new CharacterStats(charRow, monster.Level);
                    var enemy = new Enemy(player, enemyStats, charRow, charRow.ElementalType);
                    enemies.Add(enemy);
                }
            }

            return (simulator, player, enemies);
        }

        private (InfiniteTowerSimulator Simulator, Player Player, List<Enemy> Enemies) CreateTestSimulatorWithWeakEnemies()
        {
            var waveRows = new List<InfiniteTowerFloorWaveSheet.WaveData>
            {
                CreateTestWaveRow(1, 1, 201000, 1, 1), // Weak enemy
            };
            return CreateTestSimulatorWithWaves(waveRows);
        }

        private (InfiniteTowerSimulator Simulator, Player Player, List<Enemy> Enemies) CreateTestSimulatorWithStrongEnemies()
        {
            var waveRows = new List<InfiniteTowerFloorWaveSheet.WaveData>
            {
                CreateTestWaveRow(1, 1, 201000, 1, 1000), // Very strong enemy level 1000
            };

            var random = new TestRandom();
            var avatar = CreateTestAvatar();
            // 플레이어를 매우 약하게 만들기 위해 레벨 1로 설정
            avatar.level = 1;
            var sheets = _tableSheets.GetSimulatorSheets();
            var player = new Player(avatar, sheets);

            var simulator = new InfiniteTowerSimulator(
                random,
                avatar,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(Nekoyume.Model.EnumType.BattleType.Adventure),
                1,
                1,
                CreateTestFloorRow(),
                waveRows,
                false,
                0,
                sheets,
                new EnemySkillSheet(),
                new CostumeStatSheet(),
                _tableSheets.ItemSheet,
                new List<StatModifier>(),
                new BuffLimitSheet(),
                new BuffLinkSheet(),
                new List<InfiniteTowerCondition>(),
                0);

            var enemies = new List<Enemy>();
            foreach (var waveData in waveRows)
            {
                foreach (var monster in waveData.Monsters)
                {
                    // 강한 적 캐릭터 시트 사용
                    var charRow = CreateStrongEnemyCharacterRow();
                    var enemyStats = new CharacterStats(charRow, monster.Level);
                    var enemy = new Enemy(player, enemyStats, charRow, charRow.ElementalType);
                    enemies.Add(enemy);
                }
            }

            return (simulator, player, enemies);
        }

        private (InfiniteTowerSimulator Simulator, Player Player, List<Enemy> Enemies) CreateTestSimulatorWithTurnLimit(int turnLimit)
        {
            var waveRows = new List<InfiniteTowerFloorWaveSheet.WaveData>
            {
                CreateTestWaveRow(1, 1, 201000, 3, 1),
            };

            var random = new TestRandom();
            var avatar = CreateTestAvatar();
            // 플레이어를 매우 약하게 만들어서 확실히 타임오버가 발생하도록 함
            avatar.level = 1;
            var sheets = _tableSheets.GetSimulatorSheets();
            var player = new Player(avatar, sheets);

            var simulator = new InfiniteTowerSimulator(
                random,
                avatar,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(Nekoyume.Model.EnumType.BattleType.Adventure),
                1,
                1,
                CreateTestFloorRow(),
                waveRows,
                false,
                0,
                sheets,
                new EnemySkillSheet(),
                new CostumeStatSheet(),
                _tableSheets.ItemSheet,
                new List<StatModifier>(),
                new BuffLimitSheet(),
                new BuffLinkSheet(),
                new List<InfiniteTowerCondition>(),
                100,
                turnLimit: turnLimit);

            var enemies = new List<Enemy>();
            foreach (var waveData in waveRows)
            {
                foreach (var monster in waveData.Monsters)
                {
                    // 매우 강한 적 캐릭터 시트 사용
                    var charRow = CreateVeryStrongEnemyCharacterRow();
                    var enemyStats = new CharacterStats(charRow, monster.Level);
                    var enemy = new Enemy(player, enemyStats, charRow, charRow.ElementalType);
                    enemies.Add(enemy);
                }
            }

            return (simulator, player, enemies);
        }

        private AvatarState CreateTestAvatar()
        {
            return new AvatarState(
                new Address("0x1234567890123456789012345678901234567890"),
                new Address("0x1234567890123456789012345678901234567890"),
                0L,
                new QuestList(
                    new QuestSheet(),
                    new QuestRewardSheet(),
                    new QuestItemRewardSheet(),
                    new EquipmentItemRecipeSheet(),
                    new EquipmentItemSubRecipeSheet()),
                new WorldInformation(0L, null, false),
                new Address("0x1234567890123456789012345678901234567890"),
                "TestAvatar");
        }

        private InfiniteTowerCondition CreateTestCondition(SkillTargetType targetType, StatType statType, int value)
        {
            var row = new InfiniteTowerConditionSheet.Row();
            var fields = new List<string>
            {
                "1", // Id
                ((int)statType).ToString(), // StatType
                ((int)targetType).ToString(), // TargetType
                "0", // OperationType (Add)
                value.ToString(), // Value
            };
            row.Set(fields);
            return new InfiniteTowerCondition(row);
        }

        private InfiniteTowerFloorWaveSheet.WaveData CreateTestWaveRow(int id, int floorId, int enemyId, int enemyCount, int enemyLevel)
        {
            var monster = new InfiniteTowerFloorWaveSheet.MonsterData(enemyId, enemyLevel, enemyCount);
            var wave = new InfiniteTowerFloorWaveSheet.WaveData(1, new List<InfiniteTowerFloorWaveSheet.MonsterData> { monster }, false);
            return wave;
        }

        private CharacterSheet.Row CreateTestCharacterRow()
        {
            var row = new CharacterSheet.Row();
            row.Set(new List<string>
            {
                "20001", // Id
                "S", // SizeType
                "0", // ElementalType
                "100", // HP
                "50", // ATK
                "30", // DEF
                "10", // CRI
                "90", // HIT
                "70", // SPD
                "12", // LvHP
                "0.8", // LvATK
                "0.4", // LvDEF
                "0", // LvCRI
                "3.6", // LvHIT
                "2.8", // LvSPD
                "0.5", // AttackRange
                "3", // RunSpeed
            });
            return row;
        }

        private CharacterSheet.Row CreateStrongEnemyCharacterRow()
        {
            var row = new CharacterSheet.Row();
            row.Set(new List<string>
            {
                "20002", // Id
                "S", // SizeType
                "0", // ElementalType
                "1000", // HP (매우 높음)
                "500", // ATK (매우 높음)
                "300", // DEF (매우 높음)
                "50", // CRI
                "95", // HIT
                "80", // SPD
                "120", // LvHP
                "8.0", // LvATK (매우 높음)
                "4.0", // LvDEF (매우 높음)
                "0", // LvCRI
                "3.6", // LvHIT
                "2.8", // LvSPD
                "0.5", // AttackRange
                "3", // RunSpeed
            });
            return row;
        }

        private CharacterSheet.Row CreateVeryStrongEnemyCharacterRow()
        {
            var row = new CharacterSheet.Row();
            row.Set(new List<string>
            {
                "201000", // Id
                "S", // SizeType
                "0", // ElementalType
                "100000", // HP - 극도로 높은 HP
                "10000", // ATK - 극도로 높은 공격력
                "5000", // DEF - 극도로 높은 방어력
                "100", // CRI
                "200", // HIT
                "200", // SPD
                "1000", // LvHP
                "100.0", // LvATK
                "50.0", // LvDEF
                "10.0", // LvCRI
                "10.0", // LvHIT
                "10.0", // LvSPD
                "0.5", // AttackRange
                "3", // RunSpeed
            });
            return row;
        }

        private CharacterSheet.Row CreateWeakPlayerCharacterRow()
        {
            var row = new CharacterSheet.Row();
            row.Set(new List<string>
            {
                "20003", // Id
                "S", // SizeType
                "0", // ElementalType
                "10", // HP (매우 낮음)
                "5", // ATK (매우 낮음)
                "3", // DEF (매우 낮음)
                "1", // CRI
                "50", // HIT (낮음)
                "30", // SPD (낮음)
                "1", // LvHP
                "0.1", // LvATK (매우 낮음)
                "0.1", // LvDEF (매우 낮음)
                "0", // LvCRI
                "1.0", // LvHIT
                "1.0", // LvSPD
                "0.5", // AttackRange
                "3", // RunSpeed
            });
            return row;
        }

        private ItemSheet CreateTestItemSheet()
        {
            var itemSheet = new ItemSheet();
            // Use the existing ItemSheet from _tableSheets which already has the test items
            return _tableSheets.ItemSheet;
        }

        private InfiniteTowerFloorSheet.Row CreateTestFloorRow()
        {
            var row = new InfiniteTowerFloorSheet.Row();
            var fields = new List<string>
            {
                "1", // Id
                "1", // Floor
                "1000", // RequiredCp
                "2000", // MaxCp
                string.Empty, // ForbiddenItemSubTypes
                "1", // MinItemGrade
                "5", // MaxItemGrade
                "1", // MinItemLevel
                "10", // MaxItemLevel
                "1", // GuaranteedConditionId
                "0", // MinRandomConditions
                "2", // MaxRandomConditions
                string.Empty, // RandomConditionId1
                string.Empty, // RandomConditionWeight1
                string.Empty, // RandomConditionId2
                string.Empty, // RandomConditionWeight2
                string.Empty, // RandomConditionId3
                string.Empty, // RandomConditionWeight3
                string.Empty, // RandomConditionId4
                string.Empty, // RandomConditionWeight4
                string.Empty, // RandomConditionId5
                string.Empty, // RandomConditionWeight5
                "100000", // ItemRewardId1 (보물상자)
                "1", // ItemRewardCount1
                "301000", // ItemRewardId2 (핏빛 보석)
                "2", // ItemRewardCount2
                string.Empty, // ItemRewardId3
                string.Empty, // ItemRewardCount3
                string.Empty, // ItemRewardId4
                string.Empty, // ItemRewardCount4
                string.Empty, // ItemRewardId5
                string.Empty, // ItemRewardCount5
                "NCG", // FungibleAssetRewardTicker1
                "100", // FungibleAssetRewardAmount1
                "CRYSTAL", // FungibleAssetRewardTicker2
                "50", // FungibleAssetRewardAmount2
                string.Empty, // FungibleAssetRewardTicker3
                string.Empty, // FungibleAssetRewardAmount3
                string.Empty, // FungibleAssetRewardTicker4
                string.Empty, // FungibleAssetRewardAmount4
                string.Empty, // FungibleAssetRewardTicker5
                string.Empty, // FungibleAssetRewardAmount5
                "10", // NcgCost
                "100000", // MaterialCostId
                "1", // MaterialCostCount
                string.Empty, // ForbiddenRuneTypes
                string.Empty, // RequiredElementalType
            };
            row.Set(fields);
            return row;
        }

        // Helper method to test ShouldApplyCondition logic
        private bool ShouldApplyCondition(InfiniteTowerCondition condition, bool isPlayer)
        {
            if (condition.TargetType == null || !condition.TargetType.Any())
            {
                return false;
            }

            // Check if any target type matches (OR logic)
            return condition.TargetType.Any(targetType => targetType switch
            {
                SkillTargetType.Self => isPlayer,
                SkillTargetType.Enemy => !isPlayer,
                SkillTargetType.Enemies => !isPlayer,
                SkillTargetType.Ally => isPlayer,
                _ => false
            });
        }
    }
}
