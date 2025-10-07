namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Lib9c.Model.State;
    using Xunit;

    public class CpStateTest
    {
        [Fact]
        public void Serialize_WithLongValue_ShouldSerializeCorrectly()
        {
            // Arrange
            long cpValue = 123456789L;
            var cpState = new CpState(cpValue);

            // Act
            var serialized = cpState.Serialize();

            // Assert
            Assert.IsType<List>(serialized);
            var list = (List)serialized;
            Assert.Single(list);
            Assert.Equal(cpValue, list[0].ToLong());
        }

        [Fact]
        public void Deserialize_FromIntValue_ShouldDeserializeToLong()
        {
            // Arrange - Simulate old int serialization
            int oldCpValue = 123456789;
            var oldSerialized = List.Empty.Add(oldCpValue.Serialize());

            // Act
            var cpState = new CpState(oldSerialized);

            // Assert
            Assert.Equal((long)oldCpValue, cpState.Cp);
        }

        [Theory]
        [InlineData(987654321L)]
        [InlineData(555666777L)]
        [InlineData(999999999999L)]
        [InlineData(0L)]
        [InlineData(-123456L)]
        public void Deserialize_FromLongValue_ShouldDeserializeCorrectly(long cpValue)
        {
            // Arrange
            var serialized = List.Empty.Add(cpValue.Serialize());

            // Act
            var cpState = new CpState(serialized);

            // Assert
            Assert.Equal(cpValue, cpState.Cp);
        }

        [Theory]
        [InlineData(555666777L)]
        [InlineData(999999999999L)]
        [InlineData(0L)]
        [InlineData(-123456L)]
        public void SerializeAndDeserialize_RoundTrip_ShouldPreserveValue(long originalCp)
        {
            // Arrange
            var originalState = new CpState(originalCp);

            // Act
            var serialized = originalState.Serialize();
            var deserializedState = new CpState(serialized);

            // Assert
            Assert.Equal(originalCp, deserializedState.Cp);
        }

        [Fact]
        public void SerializeAndDeserialize_WithMaxIntValue_ShouldHandleCorrectly()
        {
            // Arrange
            int maxIntValue = int.MaxValue;
            var oldSerialized = List.Empty.Add(maxIntValue.Serialize());

            // Act
            var cpState = new CpState(oldSerialized);

            // Assert
            Assert.Equal((long)maxIntValue, cpState.Cp);
        }

        [Theory]
        [InlineData(123456L)]
        [InlineData(789012L)]
        [InlineData(0L)]
        public void Constructor_WithLongValue_ShouldSetCpCorrectly(long cpValue)
        {
            // Arrange & Act
            var cpState = new CpState(cpValue);

            // Assert
            Assert.Equal(cpValue, cpState.Cp);
        }

        [Fact]
        public void Bencoded_ShouldReturnSerializedValue()
        {
            // Arrange
            long cpValue = 789012L;
            var cpState = new CpState(cpValue);

            // Act
            var bencoded = cpState.Bencoded;

            // Assert
            Assert.IsType<List>(bencoded);
            var list = (List)bencoded;
            Assert.Single(list);
            Assert.Equal(cpValue, list[0].ToLong());
        }
    }
}
