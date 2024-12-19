namespace Lib9c.Tests.TableData.Event
{
    using System.Linq;
    using Nekoyume.TableData.Event;
    using Xunit;

    public class PatrolRewardSheetTest
    {
        private readonly PatrolRewardSheet _sheet = new ();

        public PatrolRewardSheetTest()
        {
            const string csv =
                "id,start,end,interval,min_level,max_level,reward_count,reward_item_id,reward_ticker,reward_count,reward_item_id,reward_ticker,reward_count,reward_item_id,reward_ticker,reward_count,reward_item_id,reward_ticker\n1,0,100,8400,1,200,1,500000,,1,600201,\n2,100,200,8400,201,350,1,500000,,2,600201,,100000,,CRYSTAL\n3,200,300,8400,351,,2,500000,,3,600201,,200000,,CRYSTAL,1,600202,\n";
            _sheet.Set(csv);
        }

        [Fact]
        public void Set()
        {
            var row = _sheet[1];
            Assert.Equal(0L, row.StartedBlockIndex);
            Assert.Equal(100L, row.EndedBlockIndex);
            Assert.Equal(8400L, row.Interval);
            Assert.Equal(1, row.MinimumLevel);
            Assert.Equal(200, row.MaxLevel);
            var apReward = row.Rewards.First();
            Assert.Equal(1, apReward.Count);
            Assert.Equal(500000, apReward.ItemId);
            Assert.True(string.IsNullOrEmpty(apReward.Ticker));
            var gdReward = row.Rewards.Last();
            Assert.Equal(1, gdReward.Count);
            Assert.Equal(600201, gdReward.ItemId);
            Assert.True(string.IsNullOrEmpty(gdReward.Ticker));

            row = _sheet[2];
            Assert.Equal(100L, row.StartedBlockIndex);
            Assert.Equal(200L, row.EndedBlockIndex);
            Assert.Equal(8400L, row.Interval);
            Assert.Equal(201, row.MinimumLevel);
            Assert.Equal(350, row.MaxLevel);
            apReward = row.Rewards.First();
            Assert.Equal(1, apReward.Count);
            Assert.Equal(500000, apReward.ItemId);
            Assert.True(string.IsNullOrEmpty(apReward.Ticker));
            gdReward = row.Rewards[1];
            Assert.Equal(2, gdReward.Count);
            Assert.Equal(600201, gdReward.ItemId);
            Assert.True(string.IsNullOrEmpty(gdReward.Ticker));
            var crystalReward = row.Rewards.Last();
            Assert.Equal(100000, crystalReward.Count);
            Assert.Equal(0, crystalReward.ItemId);
            Assert.Equal("CRYSTAL", crystalReward.Ticker);

            row = _sheet[3];
            Assert.Equal(200L, row.StartedBlockIndex);
            Assert.Equal(300L, row.EndedBlockIndex);
            Assert.Equal(8400L, row.Interval);
            Assert.Equal(351, row.MinimumLevel);
            Assert.Null(row.MaxLevel);
            apReward = row.Rewards.First();
            Assert.Equal(2, apReward.Count);
            Assert.Equal(500000, apReward.ItemId);
            Assert.True(string.IsNullOrEmpty(apReward.Ticker));
            gdReward = row.Rewards[1];
            Assert.Equal(3, gdReward.Count);
            Assert.Equal(600201, gdReward.ItemId);
            Assert.True(string.IsNullOrEmpty(gdReward.Ticker));
            crystalReward = row.Rewards[2];
            Assert.Equal(200000, crystalReward.Count);
            Assert.Equal(0, crystalReward.ItemId);
            Assert.Equal("CRYSTAL", crystalReward.Ticker);
            var rdReward = row.Rewards.Last();
            Assert.Equal(1, rdReward.Count);
            Assert.Equal(600202, rdReward.ItemId);
            Assert.True(string.IsNullOrEmpty(rdReward.Ticker));
        }

        [Theory]
        [InlineData(2, 1, 0)]
        [InlineData(350, 2, 150)]
        [InlineData(500, 3, 300)]
        public void FindByLevel(int level, int id, long blockIndex)
        {
            Assert.Equal(id, _sheet.FindByLevel(level, blockIndex).Id);
        }
    }
}
