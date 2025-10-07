namespace Lib9c.Tests.TableData.Event
{
    using Lib9c.TableData.Event;
    using Xunit;

    public class ClaimableGiftsSheetTest
    {
        [Fact]
        public void Set()
        {
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,item_2_id,item_2_quantity,item_2_tradable,item_3_id,item_3_quantity,item_3_tradable,item_4_id,item_4_quantity,item_4_tradable,item_5_id,item_5_quantity,item_5_tradable
1,1,250,600402,5,true,,,,,,,,,,,,
2,251,500,40100030,1,true,,,,,,,,,,,,
3,501,1000,49900022,1,true,,,,,,,,,,,,
4,1001,1500,40100028,1,true,,,,,,,,,,,,
5,1501,2000,400000,5,false,,,,,,,,,,,,";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            Assert.Equal(5, sheet.Count);
            Assert.NotNull(sheet.First);
            Assert.NotNull(sheet.Last);
            var row = sheet.First;
            Assert.Equal(1, row.Id);
            Assert.Equal(1, row.StartedBlockIndex);
            Assert.Equal(250, row.EndedBlockIndex);
            Assert.Single(row.Items);
            Assert.Equal(600402, row.Items[0].itemId);
            Assert.Equal(5, row.Items[0].quantity);
            Assert.True(row.Items[0].tradable);
        }
    }
}
