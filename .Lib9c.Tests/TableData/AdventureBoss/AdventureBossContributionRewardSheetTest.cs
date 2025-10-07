namespace Lib9c.Tests.TableData.AdventureBoss
{
    using System.Linq;
    using Lib9c.TableData.AdventureBoss;
    using Xunit;

    public class AdventureBossContributionRewardSheetTest
    {
        [Theory]
        [InlineData(
            @"id,adventure_boss_id,reward_1_type,reward_1_id,reward_1_ratio,reward_2_type,reward_2_id,reward_2_ratio,reward_3_type,reward_3_id,reward_3_ratio,reward_4_type,reward_4_id,reward_4_ratio
1,1,Material,600201,5,,,,,,,,,",
            1)]
        [InlineData(
            @"id,adventure_boss_id,reward_1_type,reward_1_id,reward_1_ratio,reward_2_type,reward_2_id,reward_2_ratio,reward_3_type,reward_3_id,reward_3_ratio,reward_4_type,reward_4_id,reward_4_ratio
1,1,Material,600201,5,Rune,30001,5,,,,,,",
            2)]
        [InlineData(
            @"id,adventure_boss_id,reward_1_type,reward_1_id,reward_1_ratio,reward_2_type,reward_2_id,reward_2_ratio,reward_3_type,reward_3_id,reward_3_ratio,reward_4_type,reward_4_id,reward_4_ratio
1,1,Material,600201,5,Rune,30001,5,Material,600202,5,,,",
            3)]
        [InlineData(
            @"id,adventure_boss_id,reward_1_type,reward_1_id,reward_1_ratio,reward_2_type,reward_2_id,reward_2_ratio,reward_3_type,reward_3_id,reward_3_ratio,reward_4_type,reward_4_id,reward_4_ratio
1,1,Material,600201,5,Rune,30001,5,Material,600202,5,Rune,20001,5",
            4)]
        public void Set(string csv, int expectedRewardCount)
        {
            var sheet = new AdventureBossContributionRewardSheet();
            sheet.Set(csv);

            var row = sheet.First().Value;

            Assert.Equal(1, row.Id);
            Assert.Equal(1, row.AdventureBossId);
            Assert.Equal(expectedRewardCount, row.Rewards.Count);
        }
    }
}
