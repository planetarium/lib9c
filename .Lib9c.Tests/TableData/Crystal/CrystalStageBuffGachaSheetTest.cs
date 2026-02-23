namespace Lib9c.Tests.TableData.Crystal
{
    using System.Linq;
    using Nekoyume.TableData.Crystal;
    using Xunit;

    public class CrystalStageBuffGachaSheetTest
    {
        [Fact]
        public void Constructor()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());

            Assert.NotNull(tableSheets.CrystalStageBuffGachaSheet);
        }

        [Fact]
        public void Set()
        {
            var sheet = new CrystalStageBuffGachaSheet();
            sheet.Set(
                @"stage_id,max_star,normal_cost,advanced_cost
1,5,10,30");

            Assert.Equal(2, sheet.Values.Count);

            Assert.True(sheet.TryGetValue(1, out var row));

            Assert.Equal(1, row.StageId);
            Assert.Equal(5, row.MaxStar);
            Assert.Equal(10, row.NormalCost);
            Assert.Equal(30, row.AdvancedCost);

            Assert.True(sheet.TryGetValue(451, out var extendedRow));
            Assert.Equal(451, extendedRow.StageId);
            Assert.Equal(5, extendedRow.MaxStar);
            Assert.Equal(10, extendedRow.NormalCost);
            Assert.Equal(30, extendedRow.AdvancedCost);
        }
    }
}
