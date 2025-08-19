namespace Lib9c.Tests.Model.Arena
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.State;
    using Xunit;

    public class ArenaResultTest
    {
        [Fact]
        public void Deserialize_FromIntCp_ShouldDeserializeToLong()
        {
            // Arrange - Simulate old int serialization for Cp
            int oldCpValue = 123456789;
            var serialized = List.Empty
                .Add(true.Serialize()) // IsVictory
                .Add(100) // PortraitId
                .Add(50) // Level
                .Add(oldCpValue); // Cp (old int)

            // Act
            var arenaResult = new ArenaResult(serialized);

            // Assert
            Assert.Equal((long)oldCpValue, arenaResult.Cp);
        }

        [Theory]
        [InlineData(987654321L)]
        [InlineData(555666777L)]
        [InlineData(-int.MaxValue)]
        [InlineData(999999999999L)]
        public void Deserialize_ShouldDeserializeCorrectly(long cpValue)
        {
            // Arrange
            var serialized = List.Empty
                .Add(true.Serialize()) // IsVictory
                .Add(100) // PortraitId
                .Add(50) // Level
                .Add(cpValue); // Cp

            // Act
            var arenaResult = new ArenaResult(serialized);

            // Assert
            Assert.Equal(cpValue, arenaResult.Cp);
        }

        [Theory]
        [InlineData(555666777L)]
        [InlineData(999999999999L)]
        [InlineData(0L)]
        [InlineData(-123456L)]
        public void SerializeAndDeserialize_RoundTrip_ShouldPreserveCp(long originalCp)
        {
            // Arrange
            var originalArenaResult = new ArenaResult(true, 100, 50, originalCp);

            // Act
            var serialized = originalArenaResult.Serialize();
            var deserializedArenaResult = new ArenaResult((List)serialized);

            // Assert
            Assert.Equal(originalCp, deserializedArenaResult.Cp);
        }

        [Fact]
        public void SerializeAndDeserialize_WithMaxIntCp_ShouldHandleCorrectly()
        {
            // Arrange
            int maxIntValue = int.MaxValue;
            var serialized = List.Empty
                .Add(true.Serialize()) // IsVictory
                .Add(100) // PortraitId
                .Add(50) // Level
                .Add(maxIntValue); // Cp (old int)

            // Act
            var arenaResult = new ArenaResult(serialized);

            // Assert
            Assert.Equal((long)maxIntValue, arenaResult.Cp);
        }

        [Theory]
        [InlineData(123456L)]
        [InlineData(789012L)]
        [InlineData(0L)]
        public void Constructor_WithLongCp_ShouldSetCpCorrectly(long cp)
        {
            // Arrange & Act
            var arenaResult = new ArenaResult(true, 100, 50, cp);

            // Assert
            Assert.Equal(cp, arenaResult.Cp);
        }

        [Theory]
        [InlineData(789012L)]
        [InlineData(0L)]
        [InlineData(-123456L)]
        public void Serialize_WithLongCp_ShouldSerializeCorrectly(long cp)
        {
            // Arrange
            var arenaResult = new ArenaResult(true, 100, 50, cp);

            // Act
            var serialized = arenaResult.Serialize();

            // Assert
            Assert.IsType<List>(serialized);
            var list = (List)serialized;
            Assert.Equal(4, list.Count);
            Assert.Equal(cp, (long)(Integer)list[3]);
        }
    }
}
