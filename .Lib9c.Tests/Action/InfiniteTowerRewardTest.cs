namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class InfiniteTowerRewardTest
    {
        [Fact]
        public void InfiniteTowerFloorSheet_WithRewardFields_ShouldParseCorrectly()
        {
            // Arrange
            var fields = new List<string>
            {
                "1", // Id
                "1", // Floor
                "1000", // RequiredCp
                "2000", // MaxCp
                "6:7", // ForbiddenItemSubTypes (Weapon=6, Armor=7)
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
                "101", // ItemRewardId1
                "1", // ItemRewardCount1
                "102", // ItemRewardId2
                "2", // ItemRewardCount2
                string.Empty, // ItemRewardId3
                string.Empty, // ItemRewardCount3
                string.Empty, // ItemRewardId4
                string.Empty, // ItemRewardCount4
                string.Empty, // ItemRewardId5
                string.Empty, // ItemRewardCount5
                "GOLD", // FungibleAssetRewardTicker1
                "100", // FungibleAssetRewardAmount1
                "SILVER", // FungibleAssetRewardTicker2
                "200", // FungibleAssetRewardAmount2
                string.Empty, // FungibleAssetRewardTicker3
                string.Empty, // FungibleAssetRewardAmount3
                string.Empty, // FungibleAssetRewardTicker4
                string.Empty, // FungibleAssetRewardAmount4
                string.Empty, // FungibleAssetRewardTicker5
                string.Empty, // FungibleAssetRewardAmount5
                "100", // NcgCost
                "600201", // MaterialCostId
                "50", // MaterialCostCount
                string.Empty, // ForbiddenRuneTypes
                string.Empty, // RequiredElementalType
            };

            // Act
            var row = new InfiniteTowerFloorSheet.Row();
            row.Set(fields);

            // Assert
            Assert.Equal(1, row.Id);
            Assert.Equal(1, row.Floor);
            Assert.Equal(1000, row.RequiredCp);
            Assert.Equal(2000, row.MaxCp);
            Assert.Equal(1, row.GuaranteedConditionId);
            Assert.Equal(0, row.MinRandomConditions);
            Assert.Equal(2, row.MaxRandomConditions);
            // Check item rewards
            var itemRewards = row.GetItemRewards();
            Assert.Equal(2, itemRewards.Count);
            Assert.Equal(101, itemRewards[0].itemId);
            Assert.Equal(1, itemRewards[0].count);
            Assert.Equal(102, itemRewards[1].itemId);
            Assert.Equal(2, itemRewards[1].count);

            // Check fungible asset rewards
            var fungibleAssetRewards = row.GetFungibleAssetRewards();
            Assert.Equal(2, fungibleAssetRewards.Count);
            Assert.Equal("GOLD", fungibleAssetRewards[0].ticker);
            Assert.Equal(100, fungibleAssetRewards[0].amount);
            Assert.Equal("SILVER", fungibleAssetRewards[1].ticker);
            Assert.Equal(200, fungibleAssetRewards[1].amount);
        }

        [Fact]
        public void InfiniteTowerFloorSheet_WithEmptyRewards_ShouldParseCorrectly()
        {
            // Arrange
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
                "100", // NcgCost
                "600201", // MaterialCostId
                "50", // MaterialCostCount
                string.Empty, // ForbiddenRuneTypes
                string.Empty, // RequiredElementalType
            };

            // Act
            var row = new InfiniteTowerFloorSheet.Row();
            row.Set(fields);

            // Assert
            var itemRewards = row.GetItemRewards();
            var fungibleAssetRewards = row.GetFungibleAssetRewards();
            Assert.Empty(itemRewards);
            Assert.Empty(fungibleAssetRewards);
        }

        [Fact]
        public void InfiniteTowerFloorSheet_WithPartialRewards_ShouldParseCorrectly()
        {
            // Arrange
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
                "101", // ItemRewardId1
                "1", // ItemRewardCount1
                string.Empty, // ItemRewardId2
                string.Empty, // ItemRewardCount2
                string.Empty, // ItemRewardId3
                string.Empty, // ItemRewardCount3
                string.Empty, // ItemRewardId4
                string.Empty, // ItemRewardCount4
                string.Empty, // ItemRewardId5
                string.Empty, // ItemRewardCount5
                "NCG", // FungibleAssetRewardTicker1
                "50", // FungibleAssetRewardAmount1
                string.Empty, // FungibleAssetRewardTicker2
                string.Empty, // FungibleAssetRewardAmount2
                string.Empty, // FungibleAssetRewardTicker3
                string.Empty, // FungibleAssetRewardAmount3
                string.Empty, // FungibleAssetRewardTicker4
                string.Empty, // FungibleAssetRewardAmount4
                string.Empty, // FungibleAssetRewardTicker5
                string.Empty, // FungibleAssetRewardAmount5
                "100", // NcgCost
                "600201", // MaterialCostId
                "50", // MaterialCostCount
                string.Empty, // ForbiddenRuneTypes
                string.Empty, // RequiredElementalType
            };

            // Act
            var row = new InfiniteTowerFloorSheet.Row();
            row.Set(fields);

            // Assert
            var itemRewards = row.GetItemRewards();
            var fungibleAssetRewards = row.GetFungibleAssetRewards();

            Assert.Single(itemRewards);
            Assert.Equal(101, itemRewards[0].itemId);
            Assert.Equal(1, itemRewards[0].count);

            Assert.Single(fungibleAssetRewards);
            Assert.Equal("NCG", fungibleAssetRewards[0].ticker);
            Assert.Equal(50, fungibleAssetRewards[0].amount);
        }
    }
}
