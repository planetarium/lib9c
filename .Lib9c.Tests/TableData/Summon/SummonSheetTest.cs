namespace Lib9c.Tests.TableData.Summon
{
    using System;
    using System.Linq;
    using System.Text;
    using Lib9c.TableData.Summon;
    using Xunit;

    public class SummonSheetTest
    {
        private readonly SummonSheet _summonSheet;

        public SummonSheetTest()
        {
            if (!TableSheetsImporter.TryGetCsv(nameof(SummonSheet), out var summonCSV))
            {
                throw new Exception($"Not found sheet: {nameof(SummonSheet)}");
            }

            _summonSheet = new SummonSheet();
            _summonSheet.Set(summonCSV);
        }

        [Fact]
        public void SetToSheet()
        {
            const string content =
                @"groupID,cost_material,cost_material_count,cost_ncg,recipe1ID,recipe1ratio,recipe1ID,recipe2ratio
10001,800201,10,0,171,70,172,30";
            var sheet = new SummonSheet();
            sheet.Set(content);

            Assert.Single(sheet.Values);
            Assert.NotNull(sheet.Values.First());
            var row = sheet.Values.First();
            Assert.Equal(10001, row.GroupId);
            Assert.Equal(800201, row.CostMaterial);
            Assert.Equal(10, row.CostMaterialCount);
            Assert.Equal(0, row.CostNcg);
            Assert.Equal(2, row.Recipes.Count);
            // Recipe ID
            Assert.Contains(171, row.Recipes.Select(r => r.Item1));
            Assert.Contains(172, row.Recipes.Select(r => r.Item1));
            // Ratio
            Assert.Contains(70, row.Recipes.Select(r => r.Item2));
            Assert.Contains(30, row.Recipes.Select(r => r.Item2));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(15)]
        [InlineData(30)]
        [InlineData(31)] // Former recipe limit: 30
        [InlineData(100)]
        [InlineData(101, typeof(IndexOutOfRangeException))]
        public void RecipeLimit(int recipeCount, Type exc = null)
        {
            var sbHeader = new StringBuilder();
            var sbContent = new StringBuilder();
            sbHeader.Append("groupID,cost_material,cost_material_count,cost_ncg");
            sbContent.Append("10001,800201,10,0");
            for (var i = 1; i <= recipeCount; i++)
            {
                sbHeader.Append($",recipe{i}ID,recipe{i}ratio");
                sbContent.Append($",{100 + i},{i}");
            }

            var content = $"{sbHeader}\n{sbContent}";
            var sheet = new SummonSheet();
            if (exc is not null)
            {
                Assert.Throws(exc, () => sheet.Set(content));
            }
            else
            {
                sheet.Set(content);
                var row = sheet.Values.First();
                Assert.Equal(recipeCount, row.Recipes.Count);
            }
        }

        [Fact]
        public void Total_Cumulative_Ratio()
        {
            const string content =
                @"groupID,cost_material,cost_material_count,cost_ncg,recipe1ID,recipe1ratio,recipe1ID,recipe2ratio
10001,800201,10,0,171,70,172,30";
            var sheet = new SummonSheet();
            sheet.Set(content);
            var row = sheet.Values.First();
            Assert.Equal(100, row.TotalRatio());
            Assert.Equal(70, row.CumulativeRatio(1));
            Assert.Equal(100, row.CumulativeRatio(2));
            // If index exceeds recipe count, just return total cumulative ratio.
            Assert.Equal(100, row.CumulativeRatio(3));
        }

        [Theory]
        [InlineData(10001)]
        [InlineData(10002)]
        [InlineData(20001)]
        [InlineData(30001)]
        [InlineData(30002)]
        [InlineData(40001)]
        [InlineData(50001)]
        [InlineData(50002)]
        public void CumulativeRatio(int groupId)
        {
            var sheet = _summonSheet;
            var targetRow = sheet.OrderedList.First(r => r.GroupId == groupId);

            for (var i = 1; i <= SummonSheet.Row.MaxRecipeCount; i++)
            {
                var sum = 0;
                for (var j = 0; j < i; j++)
                {
                    if (j < targetRow.Recipes.Count)
                    {
                        sum += targetRow.Recipes[j].Item2;
                    }
                }

                Assert.Equal(sum, targetRow.CumulativeRatio(i));
            }
        }
    }
}
