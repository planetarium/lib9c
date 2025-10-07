namespace Lib9c.Tests.Model
{
    using Lib9c.Model.State;
    using Xunit;

    public class WorldBossKillRewardRecordTest
    {
        [Fact]
        public void IsClaimable()
        {
            var rewardRecord = new WorldBossKillRewardRecord();
            Assert.False(rewardRecord.IsClaimable(1));

            rewardRecord[1] = false;
            Assert.False(rewardRecord.IsClaimable(1));
            Assert.True(rewardRecord.IsClaimable(2));

            rewardRecord[1] = true;
            Assert.False(rewardRecord.IsClaimable(2));
        }
    }
}
