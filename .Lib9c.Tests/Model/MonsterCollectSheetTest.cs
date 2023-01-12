using Lib9c.TableData;
using Xunit;

namespace Lib9c.Tests.Model
{
    public class MonsterCollectSheetTest
    {
        [Fact]
        public void Set()
        {
            var sheet = new MonsterCollectionSheet();
            sheet.Set("level,required_gold,reward_id\n1,500,1");
            MonsterCollectionSheet.Row row = sheet[1];
            Assert.Equal(1, row.Level);
            Assert.Equal(500, row.RequiredGold);
            Assert.Equal(1, row.RewardId);
        }
    }
}
