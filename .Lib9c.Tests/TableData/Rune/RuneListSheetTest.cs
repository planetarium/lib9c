namespace Lib9c.Tests.TableData.Rune
{
    using System;
    using Lib9c.TableData.Rune;
    using Xunit;

    public class RuneListSheetTest
    {
        public RuneListSheetTest()
        {
            if (!TableSheetsImporter.TryGetCsv(nameof(RuneListSheet), out var csv))
            {
                throw new Exception($"Not found sheet: {nameof(RuneListSheet)}");
            }

            var runeListSheet = new RuneListSheet();
            runeListSheet.Set(csv);
        }

        [Fact]
        public void SetToSheet()
        {
            const string content =
                @"id,grade,rune_type,required_level,use_place,bonus_coef
250010001,1,1,1,7,100
        ";

            var sheet = new RuneListSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            Assert.Equal(250010001, sheet.First.Id);
            Assert.Equal(1, sheet.First.Grade);
            Assert.Equal(1, sheet.First.RuneType);
            Assert.Equal(1, sheet.First.RequiredLevel);
            Assert.Equal(7, sheet.First.UsePlace);
            Assert.Equal(100, sheet.First.BonusCoef);
        }
    }
}
