namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.InfiniteTower;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class InfiniteTowerFloorSheetBattleConditionTest
    {
        [Fact]
        public void GetBattleConditions_WithAllRestrictions_ShouldReturnAllConditions()
        {
            // Arrange
            var fields = new List<string>
            {
                "1", // Id
                "1", // Floor
                "1000", // RequiredCp
                "5000", // MaxCp
                "6:7:8", // ForbiddenItemSubTypes
                "3", // MinItemGrade
                "7", // MaxItemGrade
                "5", // MinItemLevel
                "10", // MaxItemLevel
                "1", // GuaranteedConditionId
                "1", // MinRandomConditions
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
                string.Empty, // ItemRewardId1
                string.Empty, // ItemRewardCount1
                string.Empty, // ItemRewardId2
                string.Empty, // ItemRewardCount2
                string.Empty, // ItemRewardId3
                string.Empty, // ItemRewardCount3
                string.Empty, // ItemRewardId4
                string.Empty, // ItemRewardCount4
                string.Empty, // ItemRewardId5
                string.Empty, // ItemRewardCount5
                string.Empty, // FungibleAssetRewardTicker1
                string.Empty, // FungibleAssetRewardAmount1
                string.Empty, // FungibleAssetRewardTicker2
                string.Empty, // FungibleAssetRewardAmount2
                string.Empty, // FungibleAssetRewardTicker3
                string.Empty, // FungibleAssetRewardAmount3
                string.Empty, // FungibleAssetRewardTicker4
                string.Empty, // FungibleAssetRewardAmount4
                string.Empty, // FungibleAssetRewardTicker5
                string.Empty, // FungibleAssetRewardAmount5
                string.Empty, // NcgCost
                string.Empty, // MaterialCostId
                string.Empty, // MaterialCostCount
                "1,2", // ForbiddenRuneTypes
                "1", // RequiredElementalTypes
            };

            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.Set(fields);

            // Act
            var conditions = floorRow.GetBattleConditions();

            // Assert
            Assert.Equal(6, conditions.Count);

            // Debug: Check each condition type individually
            var conditionTypes = conditions.Select(c => c.Type).ToList();
            Assert.Contains(BattleConditionType.CP, conditionTypes);
            Assert.Contains(BattleConditionType.ItemGrade, conditionTypes);
            Assert.Contains(BattleConditionType.ItemLevel, conditionTypes);
            Assert.Contains(BattleConditionType.ForbiddenRuneTypes, conditionTypes);
            Assert.Contains(BattleConditionType.RequiredElementalType, conditionTypes);
            Assert.Contains(BattleConditionType.ForbiddenItemSubTypes, conditionTypes);

            // Check CP condition
            var cpCondition = conditions.Find(c => c.Type == BattleConditionType.CP);
            Assert.NotNull(cpCondition);
            Assert.Equal(1000L, cpCondition.RequiredCp);
            Assert.Equal(5000L, cpCondition.MaxCp);

            // Check ItemGrade condition
            var itemGradeCondition = conditions.Find(c => c.Type == BattleConditionType.ItemGrade);
            Assert.NotNull(itemGradeCondition);
            Assert.Equal(3, itemGradeCondition.MinItemGrade);
            Assert.Equal(7, itemGradeCondition.MaxItemGrade);

            // Check ItemLevel condition
            var itemLevelCondition = conditions.Find(c => c.Type == BattleConditionType.ItemLevel);
            Assert.NotNull(itemLevelCondition);
            Assert.Equal(5, itemLevelCondition.MinItemLevel);
            Assert.Equal(10, itemLevelCondition.MaxItemLevel);

            // Check ForbiddenRuneTypes condition
            var runeTypesCondition = conditions.Find(c => c.Type == BattleConditionType.ForbiddenRuneTypes);
            Assert.NotNull(runeTypesCondition);
            Assert.Equal(2, runeTypesCondition.ForbiddenRuneTypes.Count);
            Assert.Contains(RuneType.Stat, runeTypesCondition.ForbiddenRuneTypes);
            Assert.Contains(RuneType.Skill, runeTypesCondition.ForbiddenRuneTypes);

            // Check RequiredElementalType condition
            var elementalCondition = conditions.Find(c => c.Type == BattleConditionType.RequiredElementalType);
            Assert.NotNull(elementalCondition);
            Assert.Single(elementalCondition.RequiredElementalTypes);
            Assert.Contains(ElementalType.Fire, elementalCondition.RequiredElementalTypes);

            // Check ForbiddenItemSubTypes condition
            var itemSubTypesCondition = conditions.Find(c => c.Type == BattleConditionType.ForbiddenItemSubTypes);
            Assert.NotNull(itemSubTypesCondition);
            Assert.Equal(3, itemSubTypesCondition.ForbiddenItemSubTypes.Count);
            Assert.Contains(ItemSubType.Weapon, itemSubTypesCondition.ForbiddenItemSubTypes);
            Assert.Contains(ItemSubType.Armor, itemSubTypesCondition.ForbiddenItemSubTypes);
            Assert.Contains(ItemSubType.Belt, itemSubTypesCondition.ForbiddenItemSubTypes);
        }

        [Fact]
        public void GetBattleConditions_WithNoRestrictions_ShouldReturnEmptyList()
        {
            // Arrange
            var fields = new List<string>
            {
                "1", // Id
                "1", // Floor
                string.Empty, // RequiredCp
                string.Empty, // MaxCp
                string.Empty, // ForbiddenItemSubTypes
                string.Empty, // MinItemGrade
                string.Empty, // MaxItemGrade
                string.Empty, // MinItemLevel
                string.Empty, // MaxItemLevel
                "1", // GuaranteedConditionId
                "1", // MinRandomConditions
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
                string.Empty, // ItemRewardId1
                string.Empty, // ItemRewardCount1
                string.Empty, // ItemRewardId2
                string.Empty, // ItemRewardCount2
                string.Empty, // ItemRewardId3
                string.Empty, // ItemRewardCount3
                string.Empty, // ItemRewardId4
                string.Empty, // ItemRewardCount4
                string.Empty, // ItemRewardId5
                string.Empty, // ItemRewardCount5
                string.Empty, // FungibleAssetRewardTicker1
                string.Empty, // FungibleAssetRewardAmount1
                string.Empty, // FungibleAssetRewardTicker2
                string.Empty, // FungibleAssetRewardAmount2
                string.Empty, // FungibleAssetRewardTicker3
                string.Empty, // FungibleAssetRewardAmount3
                string.Empty, // FungibleAssetRewardTicker4
                string.Empty, // FungibleAssetRewardAmount4
                string.Empty, // FungibleAssetRewardTicker5
                string.Empty, // FungibleAssetRewardAmount5
                string.Empty, // NcgCost
                string.Empty, // MaterialCostId
                string.Empty, // MaterialCostCount
                string.Empty, // ForbiddenRuneTypes
                string.Empty, // RequiredElementalTypes
            };

            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.Set(fields);

            // Act
            var conditions = floorRow.GetBattleConditions();

            // Assert
            Assert.Empty(conditions);
        }

        [Fact]
        public void GetBattleCondition_WithSpecificType_ShouldReturnCorrectCondition()
        {
            // Arrange
            var fields = new List<string>
            {
                "1", // Id
                "1", // Floor
                "1000", // RequiredCp
                "5000", // MaxCp
                string.Empty, // ForbiddenItemSubTypes
                "3", // MinItemGrade
                "7", // MaxItemGrade
                string.Empty, // MinItemLevel
                string.Empty, // MaxItemLevel
                "1", // GuaranteedConditionId
                "1", // MinRandomConditions
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
                string.Empty, // ItemRewardId1
                string.Empty, // ItemRewardCount1
                string.Empty, // ItemRewardId2
                string.Empty, // ItemRewardCount2
                string.Empty, // ItemRewardId3
                string.Empty, // ItemRewardCount3
                string.Empty, // ItemRewardId4
                string.Empty, // ItemRewardCount4
                string.Empty, // ItemRewardId5
                string.Empty, // ItemRewardCount5
                string.Empty, // FungibleAssetRewardTicker1
                string.Empty, // FungibleAssetRewardAmount1
                string.Empty, // FungibleAssetRewardTicker2
                string.Empty, // FungibleAssetRewardAmount2
                string.Empty, // FungibleAssetRewardTicker3
                string.Empty, // FungibleAssetRewardAmount3
                string.Empty, // FungibleAssetRewardTicker4
                string.Empty, // FungibleAssetRewardAmount4
                string.Empty, // FungibleAssetRewardTicker5
                string.Empty, // FungibleAssetRewardAmount5
                string.Empty, // NcgCost
                string.Empty, // MaterialCostId
                string.Empty, // MaterialCostCount
                string.Empty, // ForbiddenRuneTypes
                string.Empty, // RequiredElementalTypes
            };

            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.Set(fields);

            // Act & Assert
            var cpCondition = floorRow.GetBattleCondition(BattleConditionType.CP);
            Assert.NotNull(cpCondition);
            Assert.Equal(BattleConditionType.CP, cpCondition.Type);
            Assert.Equal(1000L, cpCondition.RequiredCp);
            Assert.Equal(5000L, cpCondition.MaxCp);

            var itemGradeCondition = floorRow.GetBattleCondition(BattleConditionType.ItemGrade);
            Assert.NotNull(itemGradeCondition);
            Assert.Equal(BattleConditionType.ItemGrade, itemGradeCondition.Type);
            Assert.Equal(3, itemGradeCondition.MinItemGrade);
            Assert.Equal(7, itemGradeCondition.MaxItemGrade);

            var itemLevelCondition = floorRow.GetBattleCondition(BattleConditionType.ItemLevel);
            Assert.Null(itemLevelCondition);

            var runeTypesCondition = floorRow.GetBattleCondition(BattleConditionType.ForbiddenRuneTypes);
            Assert.Null(runeTypesCondition);

            var elementalCondition = floorRow.GetBattleCondition(BattleConditionType.RequiredElementalType);
            Assert.Null(elementalCondition);

            var itemSubTypesCondition = floorRow.GetBattleCondition(BattleConditionType.ForbiddenItemSubTypes);
            Assert.Null(itemSubTypesCondition);
        }

        [Fact]
        public void ParseItemSubTypes_WithColonSeparator_ShouldWork()
        {
            // Test ParseItemSubTypes directly
            var result = Nekoyume.TableData.TableExtensions.ParseItemSubTypes("6:7:8");

            // Debug: Check what we actually get
            Assert.True(result.Count > 0, $"Expected at least 1 item, but got {result.Count}. Result: [{string.Join(", ", result)}]");
            Assert.Equal(3, result.Count);
            Assert.Contains(Nekoyume.Model.Item.ItemSubType.Weapon, result);
            Assert.Contains(Nekoyume.Model.Item.ItemSubType.Armor, result);
            Assert.Contains(Nekoyume.Model.Item.ItemSubType.Belt, result);
        }

        [Fact]
        public void ParseElementalTypes_WithColonSeparator_ShouldWork()
        {
            // Test ParseElementalTypes directly
            var result = Nekoyume.TableData.TableExtensions.ParseElementalTypes("1:2:3");

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(ElementalType.Fire, result);
            Assert.Contains(ElementalType.Water, result);
            Assert.Contains(ElementalType.Land, result);
        }

        [Fact]
        public void RequiredElementalTypes_WithMultipleTypes_ShouldParseCorrectly()
        {
            // Arrange
            var fields = new List<string>
            {
                "1", // Id
                "1", // Floor
                "1000", // RequiredCp
                "5000", // MaxCp
                string.Empty, // ForbiddenItemSubTypes
                string.Empty, // MinItemGrade
                string.Empty, // MaxItemGrade
                string.Empty, // MinItemLevel
                string.Empty, // MaxItemLevel
                "1", // GuaranteedConditionId
                "1", // MinRandomConditions
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
                string.Empty, // ItemRewardId1
                string.Empty, // ItemRewardCount1
                string.Empty, // ItemRewardId2
                string.Empty, // ItemRewardCount2
                string.Empty, // ItemRewardId3
                string.Empty, // ItemRewardCount3
                string.Empty, // ItemRewardId4
                string.Empty, // ItemRewardCount4
                string.Empty, // ItemRewardId5
                string.Empty, // ItemRewardCount5
                string.Empty, // FungibleAssetRewardTicker1
                string.Empty, // FungibleAssetRewardAmount1
                string.Empty, // FungibleAssetRewardTicker2
                string.Empty, // FungibleAssetRewardAmount2
                string.Empty, // FungibleAssetRewardTicker3
                string.Empty, // FungibleAssetRewardAmount3
                string.Empty, // FungibleAssetRewardTicker4
                string.Empty, // FungibleAssetRewardAmount4
                string.Empty, // FungibleAssetRewardTicker5
                string.Empty, // FungibleAssetRewardAmount5
                string.Empty, // NcgCost
                string.Empty, // MaterialCostId
                string.Empty, // MaterialCostCount
                string.Empty, // ForbiddenRuneTypes
                "1:2:3", // RequiredElementalTypes (Fire:Water:Land)
            };

            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.Set(fields);

            // Act
            var conditions = floorRow.GetBattleConditions();

            // Assert
            var elementalCondition = conditions.Find(c => c.Type == BattleConditionType.RequiredElementalType);
            Assert.NotNull(elementalCondition);
            Assert.Equal(3, elementalCondition.RequiredElementalTypes.Count);
            Assert.Contains(ElementalType.Fire, elementalCondition.RequiredElementalTypes);
            Assert.Contains(ElementalType.Water, elementalCondition.RequiredElementalTypes);
            Assert.Contains(ElementalType.Land, elementalCondition.RequiredElementalTypes);

            // Verify floor row directly
            Assert.Equal(3, floorRow.RequiredElementalTypes.Count);
            Assert.Contains(ElementalType.Fire, floorRow.RequiredElementalTypes);
            Assert.Contains(ElementalType.Water, floorRow.RequiredElementalTypes);
            Assert.Contains(ElementalType.Land, floorRow.RequiredElementalTypes);
        }
    }
}
