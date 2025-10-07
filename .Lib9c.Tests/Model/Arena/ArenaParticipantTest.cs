namespace Lib9c.Tests.Model.Arena
{
    using Bencodex.Types;
    using Lib9c.Model.Arena;
    using Lib9c.Model.State;
    using Libplanet.Crypto;
    using Xunit;

    public class ArenaParticipantTest
    {
        [Fact]
        public void Deserialize_FromIntCp_ShouldDeserializeToLong()
        {
            // Arrange - Simulate old int serialization for Cp
            int oldCpValue = 123456789;
            var serialized = List.Empty
                .Add(1)
                .Add(new Address("0x1234567890123456789012345678901234567890").Serialize())
                .Add("Joy")
                .Add(100) // PortraitId
                .Add(50) // Level
                .Add(oldCpValue) // Cp (old int)
                .Add(1000) // Score
                .Add(5) // TotalScore
                .Add(3) // Win
                .Add(2) // Lose
                .Add(1) // Draw
                .Add(1000L) // ClaimedBlockIndex
                .Add(2000L) // RefillBlockIndex
                .Add(3); // PurchaseCount

            // Act
            var arenaParticipant = new ArenaParticipant(serialized);

            // Assert
            Assert.Equal((long)oldCpValue, arenaParticipant.Cp);
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
                .Add(1)
                .Add(new Address("0x1234567890123456789012345678901234567890").Serialize())
                .Add("Joy")
                .Add(100) // PortraitId
                .Add(50) // Level
                .Add(cpValue) // Cp
                .Add(1000) // Score
                .Add(5) // TotalScore
                .Add(3) // Win
                .Add(2) // Lose
                .Add(1) // Draw
                .Add(1000L) // ClaimedBlockIndex
                .Add(2000L) // RefillBlockIndex
                .Add(3); // PurchaseCount

            // Act
            var arenaParticipant = new ArenaParticipant(serialized);

            // Assert
            Assert.Equal(cpValue, arenaParticipant.Cp);
        }

        [Theory]
        [InlineData(555666777L)]
        [InlineData(999999999999L)]
        [InlineData(0L)]
        [InlineData(-123456L)]
        public void SerializeAndDeserialize_RoundTrip_ShouldPreserveCp(long originalCp)
        {
            // Arrange
            var address = new Address("0x1234567890123456789012345678901234567890");
            var originalArenaParticipant = new ArenaParticipant(address)
            {
                Name = "Joy",
                PortraitId = 100,
                Level = 50,
                Cp = originalCp,
                Score = 1000,
                Ticket = 5,
                TicketResetCount = 0,
                PurchasedTicketCount = 1,
                Win = 3,
                Lose = 2,
                LastBattleBlockIndex = 1000L,
            };

            // Act
            var serialized = originalArenaParticipant.Serialize();
            var deserializedArenaParticipant = new ArenaParticipant((List)serialized);

            // Assert
            Assert.Equal(originalCp, deserializedArenaParticipant.Cp);
        }

        [Fact]
        public void SerializeAndDeserialize_WithMaxIntCp_ShouldHandleCorrectly()
        {
            // Arrange
            int maxIntValue = int.MaxValue;
            var serialized = List.Empty
                .Add(1)
                .Add(new Address("0x1234567890123456789012345678901234567890").Serialize())
                .Add("Joy")
                .Add(100) // PortraitId
                .Add(50) // Level
                .Add(maxIntValue) // Cp (old int)
                .Add(1000) // Score
                .Add(5) // TotalScore
                .Add(3) // Win
                .Add(2) // Lose
                .Add(1) // Draw
                .Add(1000L) // ClaimedBlockIndex
                .Add(2000L) // RefillBlockIndex
                .Add(3); // PurchaseCount

            // Act
            var arenaParticipant = new ArenaParticipant(serialized);

            // Assert
            Assert.Equal((long)maxIntValue, arenaParticipant.Cp);
        }

        [Theory]
        [InlineData(123456L)]
        [InlineData(789012L)]
        [InlineData(0L)]
        public void Constructor_WithLongCp_ShouldSetCpCorrectly(long cp)
        {
            // Arrange & Act
            var arenaParticipant = new ArenaParticipant
            {
                Name = "Joy",
                PortraitId = 100,
                Level = 50,
                Cp = cp,
                Score = 1000,
                Ticket = 5,
                TicketResetCount = 0,
                PurchasedTicketCount = 1,
                Win = 3,
                Lose = 2,
                LastBattleBlockIndex = 1000L,
            };

            // Assert
            Assert.Equal(cp, arenaParticipant.Cp);
        }

        [Theory]
        [InlineData(789012L)]
        [InlineData(0L)]
        [InlineData(-123456L)]
        public void Serialize_WithLongCp_ShouldSerializeCorrectly(long cp)
        {
            // Arrange
            var arenaParticipant = new ArenaParticipant(new Address("0x1234567890123456789012345678901234567890"))
            {
                Name = "Joy",
                PortraitId = 100,
                Level = 50,
                Cp = cp,
                Score = 1000,
                Ticket = 5,
                TicketResetCount = 0,
                PurchasedTicketCount = 1,
                Win = 3,
                Lose = 2,
                LastBattleBlockIndex = 1000L,
            };

            // Act
            var serialized = arenaParticipant.Serialize();

            // Assert
            Assert.IsType<List>(serialized);
            var list = (List)serialized;
            Assert.Equal(13, list.Count);
            Assert.Equal(cp, (long)(Integer)list[5]);
        }
    }
}
