namespace Lib9c.Tests.TableData.AdventureBoss
{
    using System.Linq;
    using Nekoyume.TableData.AdventureBoss;
    using Xunit;

    public class AdventureBossWantedRewardSheetTest
    {
        [Theory]
        [InlineData(
            @"id,adventure_boss_id,fixed_type,fixed_id,random_1_type,random_1_id,random_1_ratio,random_2_type,random_2_id,random_2_ratio,random_3_type,random_3_id,random_3_ratio,random_4_type,random_4_id,random_4_ratio
1,1,Material,600201,Rune,30001,50,,,,,,,,,",
            1)]
        [InlineData(
            @"id,adventure_boss_id,fixed_type,fixed_id,random_1_type,random_1_id,random_1_ratio,random_2_type,random_2_id,random_2_ratio,random_3_type,random_3_id,random_3_ratio,random_4_type,random_4_id,random_4_ratio
1,1,Material,600201,Rune,30001,50,Material,600201,60,,,,,,",
            2)]
        [InlineData(
            @"id,adventure_boss_id,fixed_type,fixed_id,random_1_type,random_1_id,random_1_ratio,random_2_type,random_2_id,random_2_ratio,random_3_type,random_3_id,random_3_ratio,random_4_type,random_4_id,random_4_ratio
1,1,Material,600201,Rune,30001,50,Material,600201,60,Rune,20001,40,,,",
            3)]
        [InlineData(
            @"id,adventure_boss_id,fixed_type,fixed_id,random_1_type,random_1_id,random_1_ratio,random_2_type,random_2_id,random_2_ratio,random_3_type,random_3_id,random_3_ratio,random_4_type,random_4_id,random_4_ratio
1,1,Material,600201,Rune,30001,50,Material,600201,60,Rune,20001,40,Material,600202,40",
            4)]
        public void Set(string csv, int expectedRandomRewardCount)
        {
            var sheet = new AdventureBossWantedRewardSheet();
            sheet.Set(csv);

            var row = sheet.First().Value;

            Assert.Equal("Material", row.FixedReward.ItemType);
            Assert.Equal(600201, row.FixedReward.ItemId);
            Assert.Equal(expectedRandomRewardCount, row.RandomRewards.Count);
        }
    }
}
