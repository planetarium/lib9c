namespace Lib9c.Tests.TableData
{
    using System.Collections.Generic;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldBossContributionRewardSheetTest
    {
        private readonly WorldBossContributionRewardSheet _sheet = new ();

        public WorldBossContributionRewardSheetTest()
        {
            const string csv =
                "boss_id,reward1_count,reward1_item_id,reward1_ticker,reward2_count,reward2_item_id,reward2_ticker,reward3_count,reward3_item_id,reward3_ticker,reward4_count,reward4_item_id,reward4_ticker,reward5_count,reward5_item_id,reward5_ticker,reward6_count,reward6_item_id,reward6_ticker,reward7_count,reward7_item_id,reward7_ticker\n900001,300340,,RUNESTONE_FENRIR4,45740,,RUNESTONE_FENRIR5,5125,,RUNESTONE_FENRIR6,24600,600201,,24600,600202,,14976300000,,CRYSTAL,1270,500000\n900002,300340,,RUNESTONE_SAEHRIMNIR4,45740,,RUNESTONE_SAEHRIMNIR5,5125,,RUNESTONE_SAEHRIMNIR6,24600,600201,,24600,600202,,14976300000,,CRYSTAL,1270,500000\n";
            _sheet.Set(csv);
        }

        [Theory]
        [InlineData(900001, "FENRIR")]
        [InlineData(900002, "SAEHRIMNIR")]
        public void Set(int bossId, string runeName)
        {
            var expectedReward = new List<WorldBossContributionRewardSheet.RewardModel>
            {
                new (300340, 0, $"RUNESTONE_{runeName}4"),
                new (45740, 0, $"RUNESTONE_{runeName}5"),
                new (5125, 0, $"RUNESTONE_{runeName}6"),
                new (24600, 600201, string.Empty),
                new (24600, 600202, string.Empty),
                new (14976300000, 0, "CRYSTAL"),
                new (1270, 500000, string.Empty),
            };
            var row = _sheet[bossId];
            for (int i = 0; i < row.Rewards.Count; i++)
            {
                var reward = row.Rewards[i];
                var expected = expectedReward[i];
                Assert.Equal(reward.Count, expected.Count);
                Assert.Equal(reward.ItemId, expected.ItemId);
                Assert.Equal(reward.Ticker, expected.Ticker);
            }
        }
    }
}
