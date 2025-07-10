namespace Lib9c.Tests.Helper
{
    using System.Numerics;
    using Nekoyume.Helper;
    using Xunit;

    /// <summary>
    /// Unit tests for NumberConversionHelper utility methods.
    /// </summary>
    public class NumberConversionHelperTest
    {
        [Theory]
        [InlineData(123, 123)]
        [InlineData(-123, -123)]
        [InlineData(0, 0)]
        [InlineData(1000, 1000)]
        [InlineData(-1000, -1000)]
        public void SafeDecimalToInt32_WithNormalValues_ReturnsCorrectValues(int input, int expected)
        {
            // Act
            int result = NumberConversionHelper.SafeDecimalToInt32(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(int.MinValue, int.MinValue)]
        public void SafeDecimalToInt32_WithBoundaryValues_ReturnsCorrectValues(int input, int expected)
        {
            // Act
            int result = NumberConversionHelper.SafeDecimalToInt32(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SafeDecimalToInt32_WithValueExceedingMaxInt_ReturnsMaxInt()
        {
            // Arrange
            decimal value = (decimal)int.MaxValue + 1;

            // Act
            int result = NumberConversionHelper.SafeDecimalToInt32(value);

            // Assert
            Assert.Equal(int.MaxValue, result);
        }

        [Fact]
        public void SafeDecimalToInt32_WithValueBelowMinInt_ReturnsMinInt()
        {
            // Arrange
            decimal value = (decimal)int.MinValue - 1;

            // Act
            int result = NumberConversionHelper.SafeDecimalToInt32(value);

            // Assert
            Assert.Equal(int.MinValue, result);
        }

        [Fact]
        public void SafeDecimalToInt32_WithVeryLargeValue_ReturnsMaxInt()
        {
            // Arrange
            decimal value = decimal.MaxValue;

            // Act
            int result = NumberConversionHelper.SafeDecimalToInt32(value);

            // Assert
            Assert.Equal(int.MaxValue, result);
        }

        [Fact]
        public void SafeDecimalToInt32_WithVerySmallValue_ReturnsMinInt()
        {
            // Arrange
            decimal value = decimal.MinValue;

            // Act
            int result = NumberConversionHelper.SafeDecimalToInt32(value);

            // Assert
            Assert.Equal(int.MinValue, result);
        }

        [Theory]
        [InlineData(123, 123L)]
        [InlineData(-123, -123L)]
        [InlineData(0, 0L)]
        [InlineData(1000, 1000L)]
        [InlineData(-1000, -1000L)]
        public void SafeDecimalToInt64_WithNormalValues_ReturnsCorrectValues(int input, long expected)
        {
            // Act
            long result = NumberConversionHelper.SafeDecimalToInt64(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(long.MaxValue, long.MaxValue)]
        [InlineData(long.MinValue, long.MinValue)]
        public void SafeDecimalToInt64_WithBoundaryValues_ReturnsCorrectValues(long input, long expected)
        {
            // Act
            long result = NumberConversionHelper.SafeDecimalToInt64(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SafeDecimalToInt64_WithValueExceedingMaxLong_ReturnsMaxLong()
        {
            // Arrange
            decimal value = (decimal)long.MaxValue + 1;

            // Act
            long result = NumberConversionHelper.SafeDecimalToInt64(value);

            // Assert
            Assert.Equal(long.MaxValue, result);
        }

        [Fact]
        public void SafeDecimalToInt64_WithValueBelowMinLong_ReturnsMinLong()
        {
            // Arrange
            decimal value = (decimal)long.MinValue - 1;

            // Act
            long result = NumberConversionHelper.SafeDecimalToInt64(value);

            // Assert
            Assert.Equal(long.MinValue, result);
        }

        [Fact]
        public void SafeDecimalToInt64_WithVeryLargeValue_ReturnsMaxLong()
        {
            // Arrange
            decimal value = decimal.MaxValue;

            // Act
            long result = NumberConversionHelper.SafeDecimalToInt64(value);

            // Assert
            Assert.Equal(long.MaxValue, result);
        }

        [Fact]
        public void SafeDecimalToInt64_WithVerySmallValue_ReturnsMinLong()
        {
            // Arrange
            decimal value = decimal.MinValue;

            // Act
            long result = NumberConversionHelper.SafeDecimalToInt64(value);

            // Assert
            Assert.Equal(long.MinValue, result);
        }

        [Theory]
        [InlineData(123, 123)]
        [InlineData(-123, -123)]
        [InlineData(0, 0)]
        [InlineData(1000, 1000)]
        [InlineData(-1000, -1000)]
        public void SafeBigIntegerToInt32_WithNormalValues_ReturnsCorrectValues(int input, int expected)
        {
            // Arrange
            BigInteger value = input;

            // Act
            int result = NumberConversionHelper.SafeBigIntegerToInt32(value);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(int.MinValue, int.MinValue)]
        public void SafeBigIntegerToInt32_WithBoundaryValues_ReturnsCorrectValues(int input, int expected)
        {
            // Arrange
            BigInteger value = input;

            // Act
            int result = NumberConversionHelper.SafeBigIntegerToInt32(value);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(-1, -1)]
        public void SafeBigIntegerToInt32_WithSpecialValues_ReturnsCorrectValues(int input, int expected)
        {
            // Arrange
            BigInteger value = input;

            // Act
            int result = NumberConversionHelper.SafeBigIntegerToInt32(value);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SafeBigIntegerToInt32_WithValueExceedingMaxInt_ReturnsMaxInt()
        {
            // Arrange
            BigInteger value = (BigInteger)int.MaxValue + 1;

            // Act
            int result = NumberConversionHelper.SafeBigIntegerToInt32(value);

            // Assert
            Assert.Equal(int.MaxValue, result);
        }

        [Fact]
        public void SafeBigIntegerToInt32_WithValueBelowMinInt_ReturnsMinInt()
        {
            // Arrange
            BigInteger value = (BigInteger)int.MinValue - 1;

            // Act
            int result = NumberConversionHelper.SafeBigIntegerToInt32(value);

            // Assert
            Assert.Equal(int.MinValue, result);
        }

        [Fact]
        public void SafeBigIntegerToInt32_WithVeryLargeValue_ReturnsMaxInt()
        {
            // Arrange
            BigInteger value = BigInteger.Pow(10, 100); // very large value

            // Act
            int result = NumberConversionHelper.SafeBigIntegerToInt32(value);

            // Assert
            Assert.Equal(int.MaxValue, result);
        }

        [Fact]
        public void SafeBigIntegerToInt32_WithVerySmallValue_ReturnsMinInt()
        {
            // Arrange
            BigInteger value = -BigInteger.Pow(10, 100); // very small value

            // Act
            int result = NumberConversionHelper.SafeBigIntegerToInt32(value);

            // Assert
            Assert.Equal(int.MinValue, result);
        }
    }
}
