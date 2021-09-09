namespace Lib9c.Tests
{
    using System;
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using Xunit;

    public class ArenaScoreHelperTest
    {
        [Fact]
        public void GetScore()
        {
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 11000, BattleLog.Result.Win), 60);
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 11000, BattleLog.Result.Lose), -30);
            
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 11001, BattleLog.Result.Win), 60);
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 11001, BattleLog.Result.Lose), -30);
            
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 9000, BattleLog.Result.Win), 1);
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 9000, BattleLog.Result.Lose), -30);
            
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 8999, BattleLog.Result.Win), 1);
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 8999, BattleLog.Result.Lose), -30);
            
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 9900, BattleLog.Result.Win), 8);
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 9901, BattleLog.Result.Win), 15);
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 10000, BattleLog.Result.Win), 15);
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 10001, BattleLog.Result.Win), 20);
            
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 9800, BattleLog.Result.Lose), -20);
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 9801, BattleLog.Result.Lose), -10);
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 10000, BattleLog.Result.Lose), -10);
            Assert.Equal(ArenaScoreHelper.GetScore(10000, 10001, BattleLog.Result.Lose), -8);
        }
    }
}
