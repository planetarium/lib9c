namespace Lib9c.Tests.TableData.WorldOpen
{
    using System.Linq;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldOpenConditionSheetTest
    {
        private const string Header = "open_id,condition_group,counter_id,threshold\n";

        [Fact]
        public void ParsesRowsAndKeepsRepeatedOpenIds()
        {
            var sheet = new WorldOpenConditionSheet();
            sheet.Set(
                Header +
                "1,1,10,100\n" +
                "1,1,11,3000000000\n" +
                "1,2,12,5\n");

            Assert.Equal(3, sheet.Count);
            var row = sheet.Values.First(r => r.CounterId == 11);
            Assert.Equal(1, row.OpenId);
            Assert.Equal(1, row.ConditionGroup);
            Assert.Equal(3000000000L, row.Threshold);
            Assert.Equal(3, sheet.Keys.Distinct().Count());
        }

        [Fact]
        public void OpenIdsReferencing()
        {
            var sheet = new WorldOpenConditionSheet();
            sheet.Set(
                Header +
                "1,1,10,100\n" +
                "2,1,10,200\n" +
                "2,2,11,5\n");

            Assert.Equal(new[] { 1, 2 }, sheet.OpenIdsReferencing(10).OrderBy(i => i));
            Assert.Equal(new[] { 2 }, sheet.OpenIdsReferencing(11));
            Assert.Empty(sheet.OpenIdsReferencing(99));
        }

        [Fact]
        public void GroupsOf()
        {
            var sheet = new WorldOpenConditionSheet();
            sheet.Set(
                Header +
                "1,1,10,100\n" +
                "1,2,11,5\n" +
                "1,1,12,7\n" +
                "2,1,10,1\n");

            var groups = sheet.GroupsOf(1).ToList();
            Assert.Equal(2, groups.Count);
            Assert.Equal(new[] { 10, 12 }, groups.Single(g => g[0].ConditionGroup == 1).Select(r => r.CounterId).OrderBy(i => i));
            Assert.Equal(new[] { 11 }, groups.Single(g => g[0].ConditionGroup == 2).Select(r => r.CounterId));
            Assert.Empty(sheet.GroupsOf(99));
        }

        [Fact]
        public void ValidateCsvAcceptsWellFormed()
        {
            WorldOpenConditionSheet.ValidateCsv(
                Header +
                "1,1,10,100\n" +
                "1,1,11,200\n" +
                "1,2,12,1\n");
        }

        [Fact]
        public void ValidateCsvAcceptsBlankLines()
        {
            // Blank lines are skipped like in the other world-open sheets; Sheet.Set keeps them as
            // invalid rows, which no lookup returns.
            var csv = Header + "1,1,10,100\n\n,,,\n ,,,\n\r\n1,2,12,1\n";
            WorldOpenConditionSheet.ValidateCsv(csv);

            var sheet = new WorldOpenConditionSheet();
            sheet.Set(csv);
            Assert.Equal(2, sheet.GroupsOf(1).Count());
        }

        [Theory]
        [InlineData("1,1,10,0")] // threshold not positive
        [InlineData("1,1,10,-5")]
        [InlineData("1,1,10,x")]
        [InlineData("0,1,10,5")] // open_id not positive
        [InlineData("1,1,0,5")] // counter_id not positive
        [InlineData("1,0,10,5")] // group not positive
        [InlineData("1,1,10")] // missing cell
        public void ValidateCsvRejects(string rows)
        {
            Assert.Throws<SheetRowValidateException>(
                () => WorldOpenConditionSheet.ValidateCsv(Header + rows + "\n"));
        }

        [Fact]
        public void ValidateCsvRejectsWrongHeader()
        {
            Assert.Throws<SheetRowValidateException>(
                () => WorldOpenConditionSheet.ValidateCsv("open_id,counter_id,condition_group,threshold\n"));
        }
    }
}
