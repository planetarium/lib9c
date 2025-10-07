namespace Lib9c.Tests.Model
{
    using Lib9c.TableData;
    using Xunit;

    public class MonsterCollectionRewardSheetTest
    {
        [Fact]
        public void SetToSheet()
        {
            var sheet = new MonsterCollectionRewardSheet();
            sheet.Set("collection_level,item_id,quantity\n1,1,1\n1,2,2");

            var row = sheet[1];
            Assert.Equal(1, row.MonsterCollectionLevel);

            var rewards = row.Rewards;
            for (var i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                Assert.Equal(i + 1, reward.ItemId);
                Assert.Equal(i + 1, reward.Quantity);
            }
        }
    }
}
