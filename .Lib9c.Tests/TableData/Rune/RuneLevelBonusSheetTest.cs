namespace Lib9c.Tests.TableData.Rune
{
    using System;
    using Lib9c.TableData.Rune;
    using Xunit;

    public class RuneLevelBonusSheetTest
    {
        public RuneLevelBonusSheetTest()
        {
            if (!TableSheetsImporter.TryGetCsv(nameof(RuneLevelBonusSheet), out var csv))
            {
                throw new Exception($"Sheet not found: {nameof(RuneLevelBonusSheet)}");
            }

            var runeLevelBonusSheet = new RuneLevelBonusSheet();
            runeLevelBonusSheet.Set(csv);
        }

        [Fact]
        public void SheetTest()
        {
            const string csvContent =
                @"id,rune_level,bonus
1,1,100
";
            var sheet = new RuneLevelBonusSheet();
            sheet.Set(csvContent);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            var row = sheet.First;
            Assert.Equal(1, row.Id);
            Assert.Equal(1, row.RuneLevel);
            Assert.Equal(100, row.Bonus);
        }
    }
}
