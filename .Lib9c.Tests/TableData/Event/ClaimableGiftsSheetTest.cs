namespace Lib9c.Tests.TableData.Event
{
    using System.Text;
    using Nekoyume.TableData;
    using Xunit;

    public class ClaimableGiftsSheetTest
    {
        [Fact]
        public void Set()
        {
            var sb = new StringBuilder();
            sb.AppendLine("id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_2_id,item_2_quantity,item_3_id,item_3_quantity,item_4_id,item_4_quantity,item_5_id,item_5_quantity");
            sb.AppendLine("1,0,250,600402,5,,,,,,,,");
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_2_id,item_2_quantity,item_3_id,item_3_quantity,item_4_id,item_4_quantity,item_5_id,item_5_quantity
1,0,250,600402,5,,,,,,,,
2,251,500,40100042,1,,,,,,,,
3,501,1000,49900022,1,,,,,,,,
4,1001,1500,40100043,1,,,,,,,,";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            Assert.Equal(4, sheet.Count);
            Assert.NotNull(sheet.First);
            Assert.NotNull(sheet.Last);
            var row = sheet.First;
            Assert.Equal(1, row.Id);
            Assert.Equal(0, row.StartedBlockIndex);
            Assert.Equal(250, row.EndedBlockIndex);
            Assert.Single(row.Items);
            Assert.Equal(600402, row.Items[0].itemId);
            Assert.Equal(5, row.Items[0].quantity);
        }
    }
}
