namespace Lib9c.Tests.TableData.WorldOpen
{
    using System.Linq;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldOpenSheetTest
    {
        private const string Header =
            "id,world_id,stage_end,prerequisite_open_id,min_block_index,fallback_block_index,effective_delay_block\n";

        [Fact]
        public void ParsesRows()
        {
            var sheet = new WorldOpenSheet();
            sheet.Set(
                Header +
                "1,101,300,,1000,5000,10\n" +
                "2,101,400,1,2000,6000,0\n");

            var row = sheet[1];
            Assert.Equal(101, row.WorldId);
            Assert.Equal(300, row.StageEnd);
            Assert.Null(row.PrerequisiteOpenId);
            Assert.Equal(1000L, row.MinBlockIndex);
            Assert.Equal(5000L, row.FallbackBlockIndex);
            Assert.Equal(10L, row.EffectiveDelayBlock);
            Assert.Equal(1, sheet[2].PrerequisiteOpenId);
        }

        [Fact]
        public void ParsesLongBlockIndexes()
        {
            var sheet = new WorldOpenSheet();
            sheet.Set(Header + "1,101,300,,3000000000,4000000000,5000000000\n");

            Assert.Equal(3000000000L, sheet[1].MinBlockIndex);
            Assert.Equal(4000000000L, sheet[1].FallbackBlockIndex);
            Assert.Equal(5000000000L, sheet[1].EffectiveDelayBlock);
        }

        [Fact]
        public void ByWorld()
        {
            var sheet = new WorldOpenSheet();
            sheet.Set(
                Header +
                "1,101,300,,1000,5000,10\n" +
                "2,102,300,,1000,5000,10\n" +
                "3,101,400,1,1000,5000,10\n");

            Assert.Equal(new[] { 1, 3 }, sheet.ByWorld(101).Select(r => r.Id).OrderBy(i => i));
            Assert.Empty(sheet.ByWorld(999));
        }

        [Fact]
        public void SetDoesNotValidateAcrossRows()
        {
            // Tests need to build an invalid sheet directly.
            var sheet = new WorldOpenSheet();
            sheet.Set(Header + "1,101,300,1,1000,5000,10\n");

            Assert.Equal(1, sheet[1].PrerequisiteOpenId);
        }

        [Fact]
        public void ValidateCsvAcceptsWellFormed()
        {
            WorldOpenSheet.ValidateCsv(
                Header +
                "1,101,300,,1000,5000,10\n" +
                "2,101,400,1,1000,5000,0\n" +
                "3,102,500,2,1000,1000,0\n");
        }

        [Fact]
        public void ValidateCsvAcceptsPrerequisiteOnAnotherWorld()
        {
            WorldOpenSheet.ValidateCsv(
                Header +
                "1,101,300,,1000,5000,10\n" +
                "2,102,350,1,1000,5000,0\n");
        }

        [Fact]
        public void ValidateCsvAcceptsChainAcrossWorlds()
        {
            // Rows out of stage order; the lowest row of world 102 depends on world 101's.
            WorldOpenSheet.ValidateCsv(
                Header +
                "4,102,700,3,1000,5000,0\n" +
                "1,101,300,,1000,5000,10\n" +
                "3,102,600,1,1000,5000,0\n" +
                "5,102,800,4,1000,5000,0\n");
        }

        [Theory]
        [InlineData("1,101,300,1,1000,5000,10")] // self reference
        [InlineData("1,101,300,2,1000,5000,10\n2,101,400,1,1000,5000,10")] // cycle
        [InlineData("1,101,300,9,1000,5000,10")] // unknown prerequisite
        [InlineData("1,101,200,2,1000,5000,10\n2,101,100,9,1000,5000,10")] // unknown prerequisite deeper in chain
        [InlineData("1,101,300,,1000,0,10")] // fallback not positive
        [InlineData("1,101,300,,-5,-1,10")] // fallback negative
        [InlineData("1,101,300,,6000,5000,10")] // min > fallback
        [InlineData("1,101,300,,1000,5000,-1")] // negative delay
        [InlineData("1,101,300,,1000,5000,5001")] // delay greater than fallback
        [InlineData("1,101,300,,1000,5000,10\n2,101,300,1,1000,5000,10")] // stage_end not increasing
        [InlineData("1,101,300,,1000,5000,10\n2,101,200,1,1000,5000,10")] // stage_end decreasing
        [InlineData("1,101,300,,1000,5000,10\n1,101,400,,1000,5000,10")] // duplicated id
        [InlineData("1,101,300,,1000,5000,10\n2,101,400,,1000,5000,10")] // later row without prerequisite
        [InlineData("1,101,300,,1000,5000,10\n2,102,400,,1000,5000,10")] // another world's row without prerequisite
        [InlineData("1,101,300,,1000,5000,10\n2,101,400,1,1000,5000,10\n3,101,500,1,1000,5000,10")] // skips the row just below
        [InlineData("1,101,300,,1000,5000,10\n2,102,350,1,1000,5000,10\n3,101,400,1,1000,5000,10")] // skips another world's row just below
        [InlineData("1,101,300,,1000,5000,10\n2,101,300,,1000,5000,10")] // same stage_end, no chain
        [InlineData("1,101,300,,1000,5000,10\n2,102,200,1,1000,5000,10")] // stage_end decreasing across worlds
        [InlineData("0,101,300,,1000,5000,10")] // id not positive
        [InlineData("1,101,300,,1000,5000")] // missing cell
        [InlineData("1,101,x,,1000,5000,10")] // not a number
        public void ValidateCsvRejects(string rows)
        {
            Assert.Throws<SheetRowValidateException>(
                () => WorldOpenSheet.ValidateCsv(Header + rows + "\n"));
        }

        [Fact]
        public void ValidateCsvRejectsWrongHeader()
        {
            Assert.Throws<SheetRowValidateException>(
                () => WorldOpenSheet.ValidateCsv("id,world_id,stage_end\n"));
        }
    }
}
