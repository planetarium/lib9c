namespace Lib9c.Tests.TableData
{
    using System;
    using Nekoyume.TableData;
    using Xunit;

    public class ISheetTest
    {
        private const string Csv = @"id,round,arena_type,start_block_index,end_block_index,required_medal_count,entrance_fee,ticket_price,additional_ticket_price
1,1,OffSeason,1,2,0,0,5,2
1,2,Season,3,4,0,100,50,20
1,3,OffSeason,5,1005284,0,0,5,2";

        [Theory]
        [InlineData(null, typeof(ArenaSheet))]
        [InlineData(typeof(ArgumentException), typeof(BuffSheet))]
        public void Set(Type exc, Type sheetType)
        {
            var sheet = (ISheet)Activator.CreateInstance(sheetType);
            if (exc is null)
            {
                sheet!.Set(Csv);
                Assert.True(sheet.Count > 0);
            }
            else
            {
                Assert.Throws(exc, () => sheet!.Set(Csv));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Validate(bool exc)
        {
            var sheet = (ISheet)Activator.CreateInstance(typeof(ArenaSheet));
            var csv = exc ? "id" : Csv;
            sheet!.Set(csv);

            if (exc)
            {
                Assert.Throws<SheetRowValidateException>(sheet.Validate);
            }
            else
            {
                sheet.Validate();
            }
        }
    }
}
