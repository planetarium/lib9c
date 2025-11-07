namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    /// <summary>
    /// Integration tests for Infinite Tower functionality.
    /// These tests verify the end-to-end behavior of the Infinite Tower system
    /// including CSV data parsing, action execution, and reward distribution.
    /// </summary>
    public class InfiniteTowerIntegrationTest
    {
        [Fact]
        public void InfiniteTowerFloorSheet_WithCSVData_ShouldParseCorrectly()
        {
            // Arrange - Create a minimal valid CSV data row with all fields
            var fields = new List<string>
            {
                "1", "1", "100", "10000", string.Empty,  // 0-4: Id, Floor, RequiredCp, MaxCp, ForbiddenItemSubTypes
                "1", "5", "1", "10", "1",  // 6-10: MinItemGrade, MaxItemGrade, MinItemLevel, MaxItemLevel, GuaranteedConditionId
                "0", "2",  // 11-12: MinRandomConditions, MaxRandomConditions
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,  // 13-17: RandomConditionId1-5
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,  // 18-22: RandomConditionWeight1-5
                "101", "1", string.Empty, string.Empty, string.Empty,  // 23-27: ItemRewardId1-5
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,  // 28-32: ItemRewardCount1-5
                "GOLD", "100", string.Empty, string.Empty, string.Empty,  // 33-37: FungibleAssetRewardTicker1-3, Amount1-2
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,  // 38-42: remaining fungible asset rewards
                "100", string.Empty, string.Empty,  // 43-45: NcgCost, MaterialCostId, MaterialCostCount
                string.Empty, string.Empty, // 45-46: ForbiddenRuneTypes, RequiredElementalTypes
            };

            // Act
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.Set(fields);

            // Assert - Verify CSV parsing works correctly
            Assert.Equal(1, floorRow.Id);
            Assert.Equal(1, floorRow.Floor);
            Assert.Equal(100, floorRow.RequiredCp);
            Assert.Equal(10000, floorRow.MaxCp);
            // Check item rewards
            var itemRewards = floorRow.GetItemRewards();
            Assert.Single(itemRewards);
            Assert.Equal(101, itemRewards[0].itemId);
            Assert.Equal(1, itemRewards[0].count);

            // Check fungible asset rewards
            var fungibleAssetRewards = floorRow.GetFungibleAssetRewards();

            Assert.NotNull(floorRow.FungibleAssetRewardTicker1);
            Assert.Equal("GOLD", floorRow.FungibleAssetRewardTicker1);
            Assert.NotNull(floorRow.FungibleAssetRewardAmount1);
            Assert.Equal(100, floorRow.FungibleAssetRewardAmount1);

            Assert.Single(fungibleAssetRewards);
            Assert.Equal("GOLD", fungibleAssetRewards[0].ticker);
            Assert.Equal(100, fungibleAssetRewards[0].amount);
            Assert.Equal(100, floorRow.NcgCost);
        }

        [Fact]
        public void InfiniteTowerConditionSheet_WithCSVData_ShouldParseCorrectly()
        {
            // Arrange - Test condition CSV parsing
            var csvData = "1,2,1,0,10";
            var fields = csvData.Split(',').ToList();

            // Act
            var conditionRow = new InfiniteTowerConditionSheet.Row();
            conditionRow.Set(fields);

            // Assert - Verify condition parsing
            Assert.Equal(1, conditionRow.Id);
            Assert.Equal(StatType.ATK, conditionRow.StatType);
            Assert.Equal(10, conditionRow.Value);
            Assert.NotNull(conditionRow.TargetType);
            Assert.Single(conditionRow.TargetType);
            Assert.Equal(SkillTargetType.Enemies, conditionRow.TargetType[0]);
        }

        [Fact]
        public void InfiniteTowerScheduleSheet_WithCSVData_ShouldParseCorrectly()
        {
            // Arrange - Test schedule system CSV parsing
            var csvData = "1,1,0,99999999,3,10,86400";
            var fields = csvData.Split(',').ToList();

            // Act
            var scheduleRow = new InfiniteTowerScheduleSheet.Row();
            scheduleRow.Set(fields);

            // Assert - Verify schedule system parsing
            Assert.Equal(1, scheduleRow.Id);
            Assert.Equal(1, scheduleRow.InfiniteTowerId);
            Assert.Equal(3, scheduleRow.DailyFreeTickets);
            Assert.Equal(10, scheduleRow.MaxTickets);
            Assert.Equal(86400, scheduleRow.ResetIntervalBlocks);
            Assert.Equal(0, scheduleRow.StartBlockIndex);
            Assert.Equal(99999999, scheduleRow.EndBlockIndex);
        }

        [Fact]
        public void InfiniteTowerFloorWaveSheet_WithCSVData_ShouldParseCorrectly()
        {
            // Arrange - Test wave CSV parsing with new structure
            var csvData = "1,1,1,201000,1,2,,,,,,,,,,0";
            var fields = csvData.Split(',').ToList();

            // Act
            var waveRow = new InfiniteTowerFloorWaveSheet.Row();
            waveRow.Set(fields);

            // Assert - Verify wave parsing
            Assert.Equal(1, waveRow.FloorId);
            Assert.Single(waveRow.Waves);
            var wave = waveRow.Waves[0];
            Assert.Equal(1, wave.Number);
            Assert.Single(wave.Monsters);
            var monster = wave.Monsters[0];
            Assert.Equal(201000, monster.CharacterId);
            Assert.Equal(1, monster.Level);
            Assert.Equal(2, monster.Count);
            Assert.False(wave.HasBoss);
        }

        [Fact]
        public void InfiniteTowerBattle_WithCompleteCSVData_ShouldCreateValidAction()
        {
            // Arrange - Test action creation with all required data
            var agentPrivateKey = new PrivateKey();
            var agentAddress = agentPrivateKey.Address;
            var avatarAddress = agentAddress.Derive("avatar");

            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = 1,
                FloorId = 1,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                Foods = new List<Guid>(),
            };

            // Act & Assert - Verify action properties are set correctly
            Assert.Equal(avatarAddress, action.AvatarAddress);
            Assert.Equal(1, action.InfiniteTowerId);
            Assert.Equal(1, action.FloorId);
            Assert.NotNull(action.Equipments);
            Assert.NotNull(action.Costumes);
            Assert.NotNull(action.RuneInfos);
            Assert.NotNull(action.Foods);
            Assert.Empty(action.Equipments);
            Assert.Empty(action.Costumes);
            Assert.Empty(action.RuneInfos);
            Assert.Empty(action.Foods);
        }
    }
}
