namespace Lib9c.Tests.TableData
{
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.TableData;
    using Xunit;

    public class RuneCostSheetTest
    {
        [Fact]
        public void RuneCostData_ContainsLevel_ShouldReturnTrue_WhenLevelIsInRange()
        {
            // Arrange
            var costData = new RuneCostSheet.RuneCostData(1, 10, 5, 100, 0, 10000);

            // Act & Assert
            Assert.True(costData.ContainsLevel(1));
            Assert.True(costData.ContainsLevel(5));
            Assert.True(costData.ContainsLevel(10));
        }

        [Fact]
        public void RuneCostData_ContainsLevel_ShouldReturnFalse_WhenLevelIsOutOfRange()
        {
            // Arrange
            var costData = new RuneCostSheet.RuneCostData(1, 10, 5, 100, 0, 10000);

            // Act & Assert
            Assert.False(costData.ContainsLevel(0));
            Assert.False(costData.ContainsLevel(11));
        }

        [Fact]
        public void RuneCostData_Properties_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var costData = new RuneCostSheet.RuneCostData(5, 15, 10, 200, 5, 9500);

            // Assert
            Assert.Equal(5, costData.LevelStart);
            Assert.Equal(15, costData.LevelEnd);
            Assert.Equal(10, costData.RuneStoneQuantity);
            Assert.Equal(200, costData.CrystalQuantity);
            Assert.Equal(5, costData.NcgQuantity);
            Assert.Equal(9500, costData.LevelUpSuccessRate);
        }

        [Fact]
        public void Row_Set_ShouldParseFieldsCorrectly()
        {
            // Arrange
            var row = new RuneCostSheet.Row();
            var fields = new[] { "10001", "5", "15", "10", "200", "5", "9500" };

            // Act
            row.Set(fields);

            // Assert
            Assert.Equal(10001, row.RuneId);
            Assert.Single(row.Cost);

            var costData = row.Cost[0];
            Assert.Equal(5, costData.LevelStart);
            Assert.Equal(15, costData.LevelEnd);
            Assert.Equal(10, costData.RuneStoneQuantity);
            Assert.Equal(200, costData.CrystalQuantity);
            Assert.Equal(5, costData.NcgQuantity);
            Assert.Equal(9500, costData.LevelUpSuccessRate);
        }

        [Fact]
        public void Row_TryGetCost_ShouldReturnTrue_WhenLevelExists()
        {
            // Arrange
            var row = new RuneCostSheet.Row();
            var fields = new[] { "10001", "5", "15", "10", "200", "5", "9500" };
            row.Set(fields);

            // Act
            var result = row.TryGetCost(10, out var costData);

            // Assert
            Assert.True(result);
            Assert.NotNull(costData);
            Assert.Equal(5, costData.LevelStart);
            Assert.Equal(15, costData.LevelEnd);
        }

        [Fact]
        public void Row_TryGetCost_ShouldReturnFalse_WhenLevelDoesNotExist()
        {
            // Arrange
            var row = new RuneCostSheet.Row();
            var fields = new[] { "10001", "5", "15", "10", "200", "5", "9500" };
            row.Set(fields);

            // Act
            var result = row.TryGetCost(20, out var costData);

            // Assert
            Assert.False(result);
            Assert.Null(costData);
        }

        [Fact]
        public void Row_TryGetCost_ShouldReturnFalse_WhenNoCostData()
        {
            // Arrange
            var row = new RuneCostSheet.Row();

            // Act
            var result = row.TryGetCost(10, out var costData);

            // Assert
            Assert.False(result);
            Assert.Null(costData);
        }

        [Fact]
        public void Row_GetCostsInRange_ShouldReturnOverlappingRanges()
        {
            // Arrange
            var row = new RuneCostSheet.Row();

            // 첫 번째 구간 추가
            var fields1 = new[] { "10001", "1", "10", "5", "100", "0", "10000" };
            row.Set(fields1);

            // 두 번째 구간 추가
            var fields2 = new[] { "10001", "5", "15", "10", "200", "5", "9500" };
            var row2 = new RuneCostSheet.Row();
            row2.Set(fields2);
            row.Cost.Add(row2.Cost[0]);

            // Act
            var costsInRange = row.GetCostsInRange(5, 12).ToList();

            // Assert
            Assert.Equal(2, costsInRange.Count);
            Assert.Contains(costsInRange, c => c.LevelStart == 1 && c.LevelEnd == 10);
            Assert.Contains(costsInRange, c => c.LevelStart == 5 && c.LevelEnd == 15);
        }

        [Fact]
        public void Row_GetCostsInRange_ShouldReturnEmpty_WhenNoOverlappingRanges()
        {
            // Arrange
            var row = new RuneCostSheet.Row();
            var fields = new[] { "10001", "1", "10", "5", "100", "0", "10000" };
            row.Set(fields);

            // Act
            var costsInRange = row.GetCostsInRange(15, 20).ToList();

            // Assert
            Assert.Empty(costsInRange);
        }

        [Fact]
        public void Row_GetCostsInRange_ShouldReturnPartialOverlappingRanges()
        {
            // Arrange
            var row = new RuneCostSheet.Row();
            var fields = new[] { "10001", "5", "15", "10", "200", "5", "9500" };
            row.Set(fields);

            // Act
            var costsInRange = row.GetCostsInRange(10, 20).ToList();

            // Assert
            Assert.Single(costsInRange);
            Assert.Equal(5, costsInRange[0].LevelStart);
            Assert.Equal(15, costsInRange[0].LevelEnd);
        }

        [Fact]
        public void RuneCostSheet_Key_ShouldReturnRuneId()
        {
            // Arrange
            var row = new RuneCostSheet.Row();
            var fields = new[] { "10001", "1", "10", "5", "100", "0", "10000" };
            row.Set(fields);

            // Act & Assert
            Assert.Equal(10001, row.Key);
        }

        [Theory]
        [InlineData(1, 10, 5, true)]
        [InlineData(5, 15, 10, true)]
        [InlineData(0, 5, 3, true)] // 3은 0-5 범위에 포함됨
        [InlineData(15, 20, 18, true)] // 18은 15-20 범위에 포함됨
        [InlineData(0, 5, 6, false)] // 6은 0-5 범위에 포함되지 않음
        [InlineData(15, 20, 14, false)] // 14는 15-20 범위에 포함되지 않음
        public void RuneCostData_ContainsLevel_WithVariousRanges(int start, int end, int testLevel, bool expected)
        {
            // Arrange
            var costData = new RuneCostSheet.RuneCostData(start, end, 5, 100, 0, 10000);

            // Act
            var result = costData.ContainsLevel(testLevel);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
