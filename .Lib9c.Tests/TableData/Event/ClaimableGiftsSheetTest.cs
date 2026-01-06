namespace Lib9c.Tests.TableData.Event
{
    using Nekoyume.TableData;
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

        [Fact]
        public void Set_WithMultipleItems()
        {
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,item_2_id,item_2_quantity,item_2_tradable,item_3_id,item_3_quantity,item_3_tradable,item_4_id,item_4_quantity,item_4_tradable,item_5_id,item_5_quantity,item_5_tradable
1,1,250,600402,5,true,40100030,10,false,49900022,3,true,40100028,2,false,400000,1,true";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            Assert.Single(sheet.Values);
            var row = sheet[1];
            Assert.Equal(5, row.Items.Count);
            Assert.Equal(600402, row.Items[0].itemId);
            Assert.Equal(5, row.Items[0].quantity);
            Assert.True(row.Items[0].tradable);
            Assert.Equal(40100030, row.Items[1].itemId);
            Assert.Equal(10, row.Items[1].quantity);
            Assert.False(row.Items[1].tradable);
            Assert.Equal(49900022, row.Items[2].itemId);
            Assert.Equal(3, row.Items[2].quantity);
            Assert.True(row.Items[2].tradable);
            Assert.Equal(40100028, row.Items[3].itemId);
            Assert.Equal(2, row.Items[3].quantity);
            Assert.False(row.Items[3].tradable);
            Assert.Equal(400000, row.Items[4].itemId);
            Assert.Equal(1, row.Items[4].quantity);
            Assert.True(row.Items[4].tradable);
        }

        [Fact]
        public void Set_WithEmptyFieldsInBetween()
        {
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,item_2_id,item_2_quantity,item_2_tradable,item_3_id,item_3_quantity,item_3_tradable,item_4_id,item_4_quantity,item_4_tradable,item_5_id,item_5_quantity,item_5_tradable
1,1,250,600402,5,true,0,0,false,49900022,3,true,0,0,false,400000,1,true";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            var row = sheet[1];
            Assert.Equal(3, row.Items.Count);
            Assert.Equal(600402, row.Items[0].itemId);
            Assert.Equal(5, row.Items[0].quantity);
            Assert.True(row.Items[0].tradable);
            Assert.Equal(49900022, row.Items[1].itemId);
            Assert.Equal(3, row.Items[1].quantity);
            Assert.True(row.Items[1].tradable);
            Assert.Equal(400000, row.Items[2].itemId);
            Assert.Equal(1, row.Items[2].quantity);
            Assert.True(row.Items[2].tradable);
        }

        [Fact]
        public void Set_WithZeroItemIdOrQuantity()
        {
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,item_2_id,item_2_quantity,item_2_tradable,item_3_id,item_3_quantity,item_3_tradable,item_4_id,item_4_quantity,item_4_tradable,item_5_id,item_5_quantity,item_5_tradable
1,1,250,0,5,true,600402,0,false,49900022,3,true,,,,,,,,";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            var row = sheet[1];
            Assert.Single(row.Items);
            Assert.Equal(49900022, row.Items[0].itemId);
            Assert.Equal(3, row.Items[0].quantity);
            Assert.True(row.Items[0].tradable);
        }

        [Fact]
        public void Set_WithNoItems()
        {
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,item_2_id,item_2_quantity,item_2_tradable,item_3_id,item_3_quantity,item_3_tradable,item_4_id,item_4_quantity,item_4_tradable,item_5_id,item_5_quantity,item_5_tradable
1,1,250,,,,,,,,,,,,,,,,";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            var row = sheet[1];
            Assert.Empty(row.Items);
            Assert.Equal(1, row.Id);
            Assert.Equal(1, row.StartedBlockIndex);
            Assert.Equal(250, row.EndedBlockIndex);
        }

        [Fact]
        public void Set_WithTradableFalse()
        {
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,item_2_id,item_2_quantity,item_2_tradable,item_3_id,item_3_quantity,item_3_tradable,item_4_id,item_4_quantity,item_4_tradable,item_5_id,item_5_quantity,item_5_tradable
1,1,250,600402,5,false,40100030,10,false,,,,,,,,,,";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            var row = sheet[1];
            Assert.Equal(2, row.Items.Count);
            Assert.False(row.Items[0].tradable);
            Assert.False(row.Items[1].tradable);
        }

        [Fact]
        public void Set_WithTradableEmptyDefaultsToTrue()
        {
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,item_2_id,item_2_quantity,item_2_tradable,item_3_id,item_3_quantity,item_3_tradable,item_4_id,item_4_quantity,item_4_tradable,item_5_id,item_5_quantity,item_5_tradable
1,1,250,600402,5,,40100030,10,true,,,,,,,,,,";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            var row = sheet[1];
            Assert.Equal(2, row.Items.Count);
            Assert.True(row.Items[0].tradable);
            Assert.True(row.Items[1].tradable);
        }

        [Fact]
        public void Validate_WithinRange()
        {
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,item_2_id,item_2_quantity,item_2_tradable,item_3_id,item_3_quantity,item_3_tradable,item_4_id,item_4_quantity,item_4_tradable,item_5_id,item_5_quantity,item_5_tradable
1,100,200,600402,5,true,,,,,,,,,,,,";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            var row = sheet[1];
            Assert.True(row.Validate(100));
            Assert.True(row.Validate(150));
            Assert.True(row.Validate(200));
        }

        [Fact]
        public void Validate_OutsideRange()
        {
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,item_2_id,item_2_quantity,item_2_tradable,item_3_id,item_3_quantity,item_3_tradable,item_4_id,item_4_quantity,item_4_tradable,item_5_id,item_5_quantity,item_5_tradable
1,100,200,600402,5,true,,,,,,,,,,,,";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            var row = sheet[1];
            Assert.False(row.Validate(99));
            Assert.False(row.Validate(201));
        }

        [Fact]
        public void TryFindRowByBlockIndex_Found()
        {
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,item_2_id,item_2_quantity,item_2_tradable,item_3_id,item_3_quantity,item_3_tradable,item_4_id,item_4_quantity,item_4_tradable,item_5_id,item_5_quantity,item_5_tradable
1,100,200,600402,5,true,,,,,,,,,,,,
2,201,300,40100030,1,true,,,,,,,,,,,,
3,301,400,49900022,1,true,,,,,,,,,,,,";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            Assert.True(sheet.TryFindRowByBlockIndex(150, out var row1));
            Assert.NotNull(row1);
            Assert.Equal(1, row1.Id);
            Assert.True(sheet.TryFindRowByBlockIndex(250, out var row2));
            Assert.NotNull(row2);
            Assert.Equal(2, row2.Id);
            Assert.True(sheet.TryFindRowByBlockIndex(350, out var row3));
            Assert.NotNull(row3);
            Assert.Equal(3, row3.Id);
        }

        [Fact]
        public void TryFindRowByBlockIndex_NotFound()
        {
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,item_2_id,item_2_quantity,item_2_tradable,item_3_id,item_3_quantity,item_3_tradable,item_4_id,item_4_quantity,item_4_tradable,item_5_id,item_5_quantity,item_5_tradable
1,100,200,600402,5,true,,,,,,,,,,,,
2,201,300,40100030,1,true,,,,,,,,,,,,";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            Assert.False(sheet.TryFindRowByBlockIndex(99, out var row1));
            Assert.Null(row1);
            Assert.False(sheet.TryFindRowByBlockIndex(301, out var row2));
            Assert.Null(row2);
        }

        [Fact]
        public void TryFindRowByBlockIndex_BoundaryValues()
        {
            const string csv = @"id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,item_2_id,item_2_quantity,item_2_tradable,item_3_id,item_3_quantity,item_3_tradable,item_4_id,item_4_quantity,item_4_tradable,item_5_id,item_5_quantity,item_5_tradable
1,100,200,600402,5,true,,,,,,,,,,,,";

            var sheet = new ClaimableGiftsSheet();
            sheet.Set(csv);
            Assert.True(sheet.TryFindRowByBlockIndex(100, out var row1));
            Assert.NotNull(row1);
            Assert.Equal(1, row1.Id);
            Assert.True(sheet.TryFindRowByBlockIndex(200, out var row2));
            Assert.NotNull(row2);
            Assert.Equal(1, row2.Id);
        }
    }
}
