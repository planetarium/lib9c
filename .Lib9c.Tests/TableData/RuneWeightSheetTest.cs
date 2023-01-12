using System.Linq;
using Xunit;

namespace Lib9c.Tests.TableData
{
    public class RuneWeightSheetTest
    {
        [Fact]
        public void Set()
        {
            var tableSheet = new TableSheets(TableSheetsImporter.ImportSheets());
            var sheet = tableSheet.RuneWeightSheet;
            foreach (var row in sheet.Values)
            {
                Assert.Equal(100m, row.RuneInfos.Sum(i => i.Weight));
            }
        }
    }
}
