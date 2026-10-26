namespace Lib9c.Tests.TableData.WorldOpen
{
    using System.Linq;
    using Nekoyume.TableData;
    using Xunit;

    public class GlobalCounterSheetTest
    {
        private const string Header = "id,type,target_begin,target_end,min_avatar_level\n";

        [Fact]
        public void ParsesRows()
        {
            var sheet = new GlobalCounterSheet();
            sheet.Set(
                Header +
                "1,STAGE_FIRST_CLEAR,100,100,50\n" +
                "2,STAGE_PLAY,1,300,0\n" +
                "3,RECIPE_CRAFT,10,20,0\n");

            var row = sheet[1];
            Assert.Equal(1, row.Id);
            Assert.Equal(GlobalCounterType.STAGE_FIRST_CLEAR, row.Type);
            Assert.Equal(100, row.TargetBegin);
            Assert.Equal(100, row.TargetEnd);
            Assert.Equal(50, row.MinAvatarLevel);
            Assert.Equal(GlobalCounterType.RECIPE_CRAFT, sheet[3].Type);
        }

        [Fact]
        public void Matching()
        {
            var sheet = new GlobalCounterSheet();
            sheet.Set(
                Header +
                "1,STAGE_FIRST_CLEAR,100,100,50\n" +
                "2,STAGE_PLAY,1,300,0\n" +
                "3,STAGE_PLAY,200,400,0\n");

            Assert.Equal(
                new[] { 2, 3 },
                sheet.Matching(GlobalCounterType.STAGE_PLAY, 250).Select(r => r.Id).OrderBy(i => i));
            Assert.Equal(
                new[] { 2 },
                sheet.Matching(GlobalCounterType.STAGE_PLAY, 1).Select(r => r.Id));
            Assert.Empty(sheet.Matching(GlobalCounterType.STAGE_PLAY, 401));
            Assert.Empty(sheet.Matching(GlobalCounterType.RECIPE_CRAFT, 250));
        }

        [Fact]
        public void ValidateCsvAcceptsWellFormed()
        {
            GlobalCounterSheet.ValidateCsv(
                Header +
                "1,STAGE_FIRST_CLEAR,100,100,50\n" +
                "2,STAGE_PLAY,1,300,0\n");
        }

        [Theory]
        [InlineData("1,STAGE_FIRST_CLEAR,100,101,0")] // first clear is a single stage
        [InlineData("1,STAGE_PLAY,300,1,0")] // begin > end
        [InlineData("1,UNKNOWN,1,1,0")] // unknown type
        [InlineData("1,STAGE_PLAY,1,1")] // missing cell
        [InlineData("1,STAGE_PLAY,x,1,0")] // not a number
        [InlineData("1,STAGE_PLAY,1,1,0\n1,STAGE_PLAY,2,2,0")] // duplicated id
        public void ValidateCsvRejects(string rows)
        {
            Assert.Throws<SheetRowValidateException>(
                () => GlobalCounterSheet.ValidateCsv(Header + rows + "\n"));
        }

        [Fact]
        public void ValidateCsvRejectsWrongHeader()
        {
            Assert.Throws<SheetRowValidateException>(
                () => GlobalCounterSheet.ValidateCsv("id,type,target_end,target_begin,min_avatar_level\n"));
        }
    }
}
