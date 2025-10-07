namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Lib9c.Model;
    using Lib9c.Model.Quest;
    using Lib9c.Model.State;
    using Lib9c.TableData.Item;
    using Lib9c.TableData.Quest;
    using Libplanet.Crypto;
    using Xunit;

    public class RaiderStateTest
    {
        [Fact]
        public void Deserialize_FromIntCp_ShouldDeserializeToLong()
        {
            // Arrange - Simulate old int serialization for Cp
            int oldCpValue = 123456789;
            var serialized = List.Empty
                .Add(1000L.Serialize()) // TotalScore
                .Add(100L.Serialize()) // HighScore
                .Add(5.Serialize()) // TotalChallengeCount
                .Add(3.Serialize()) // RemainChallengeCount
                .Add(3.Serialize()) // LatestRewardRank
                .Add(1000L.Serialize()) // ClaimedBlockIndex
                .Add(2000L.Serialize()) // RefillBlockIndex
                .Add(3.Serialize()) // PurchaseCount
                .Add(oldCpValue.Serialize()) // Cp (old int)
                .Add(50.Serialize()) // Level
                .Add(100.Serialize()) // IconId
                .Add(new Address("0x1234567890123456789012345678901234567890").Serialize()) // AvatarAddress
                .Add("TestAvatar".Serialize()) // AvatarName
                .Add(1.Serialize()) // LatestBossLevel
                .Add(30L.Serialize()); // UpdatedBlockIndex

            // Act
            var raiderState = new RaiderState(serialized);

            // Assert
            Assert.Equal((long)oldCpValue, raiderState.Cp);
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
                .Add(1000L.Serialize()) // TotalScore
                .Add(100L.Serialize()) // HighScore
                .Add(5.Serialize()) // TotalChallengeCount
                .Add(3.Serialize()) // RemainChallengeCount
                .Add(3.Serialize()) // LatestRewardRank
                .Add(1000L.Serialize()) // ClaimedBlockIndex
                .Add(2000L.Serialize()) // RefillBlockIndex
                .Add(3.Serialize()) // PurchaseCount
                .Add(cpValue.Serialize()) // Cp
                .Add(50.Serialize()) // Level
                .Add(100.Serialize()) // IconId
                .Add(new Address("0x1234567890123456789012345678901234567890").Serialize()) // AvatarAddress
                .Add("TestAvatar".Serialize()) // AvatarName
                .Add(1.Serialize()) // LatestBossLevel
                .Add(30L.Serialize()); // UpdatedBlockIndex

            // Act
            var raiderState = new RaiderState(serialized);

            // Assert
            Assert.Equal(cpValue, raiderState.Cp);
        }

        [Theory]
        [InlineData(555666777L)]
        [InlineData(999999999999L)]
        [InlineData(0L)]
        [InlineData(-123456L)]
        public void SerializeAndDeserialize_RoundTrip_ShouldPreserveCp(long originalCp)
        {
            // Arrange
            var originalRaiderState = new RaiderState
            {
                TotalScore = 1000L,
                HighScore = 100L,
                TotalChallengeCount = 5,
                RemainChallengeCount = 3,
                LatestRewardRank = 3,
                ClaimedBlockIndex = 1000L,
                RefillBlockIndex = 2000L,
                PurchaseCount = 3,
                Cp = originalCp,
                Level = 50,
                IconId = 100,
                AvatarAddress = new Address("0x1234567890123456789012345678901234567890"),
                AvatarName = "TestAvatar",
                LatestBossLevel = 1,
                UpdatedBlockIndex = 30L,
            };

            // Act
            var serialized = originalRaiderState.Serialize();
            var deserializedRaiderState = new RaiderState((List)serialized);

            // Assert
            Assert.Equal(originalCp, deserializedRaiderState.Cp);
        }

        [Fact]
        public void SerializeAndDeserialize_WithMaxIntCp_ShouldHandleCorrectly()
        {
            // Arrange
            int maxIntValue = int.MaxValue;
            var serialized = List.Empty
                .Add(1000L.Serialize()) // TotalScore
                .Add(100L.Serialize()) // HighScore
                .Add(5.Serialize()) // TotalChallengeCount
                .Add(3.Serialize()) // RemainChallengeCount
                .Add(3.Serialize()) // LatestRewardRank
                .Add(1000L.Serialize()) // ClaimedBlockIndex
                .Add(2000L.Serialize()) // RefillBlockIndex
                .Add(3.Serialize()) // PurchaseCount
                .Add(maxIntValue.Serialize()) // Cp (old int)
                .Add(50.Serialize()) // Level
                .Add(100.Serialize()) // IconId
                .Add(new Address("0x1234567890123456789012345678901234567890").Serialize()) // AvatarAddress
                .Add("TestAvatar".Serialize()) // AvatarName
                .Add(1.Serialize()) // LatestBossLevel
                .Add(30L.Serialize()); // UpdatedBlockIndex

            // Act
            var raiderState = new RaiderState(serialized);

            // Assert
            Assert.Equal((long)maxIntValue, raiderState.Cp);
        }

        [Theory]
        [InlineData(123456L)]
        [InlineData(789012L)]
        [InlineData(0L)]
        public void Constructor_WithLongCp_ShouldSetCpCorrectly(long cp)
        {
            // Arrange & Act
            var raiderState = new RaiderState
            {
                TotalScore = 1000L,
                HighScore = 100L,
                TotalChallengeCount = 5,
                RemainChallengeCount = 3,
                LatestRewardRank = 3,
                ClaimedBlockIndex = 1000L,
                RefillBlockIndex = 2000L,
                PurchaseCount = 3,
                Cp = cp,
                Level = 50,
                IconId = 100,
                AvatarAddress = new Address("0x1234567890123456789012345678901234567890"),
                AvatarName = "TestAvatar",
                LatestBossLevel = 1,
                UpdatedBlockIndex = 30L,
            };

            // Assert
            Assert.Equal(cp, raiderState.Cp);
        }

        [Theory]
        [InlineData(789012L)]
        [InlineData(0L)]
        [InlineData(-123456L)]
        public void Serialize_WithLongCp_ShouldSerializeCorrectly(long cp)
        {
            // Arrange
            var raiderState = new RaiderState
            {
                TotalScore = 1000L,
                HighScore = 100L,
                TotalChallengeCount = 5,
                RemainChallengeCount = 3,
                LatestRewardRank = 3,
                ClaimedBlockIndex = 1000L,
                RefillBlockIndex = 2000L,
                PurchaseCount = 3,
                Cp = cp,
                Level = 50,
                IconId = 100,
                AvatarAddress = new Address("0x1234567890123456789012345678901234567890"),
                AvatarName = "TestAvatar",
                LatestBossLevel = 1,
                UpdatedBlockIndex = 30L,
            };

            // Act
            var serialized = raiderState.Serialize();

            // Assert
            Assert.IsType<List>(serialized);
            var list = (List)serialized;
            Assert.Equal(16, list.Count);
            Assert.Equal(cp, list[8].ToLong());
        }

        [Theory]
        [InlineData(123456L)]
        [InlineData(789012L)]
        [InlineData(0L)]
        public void Update_WithLongCp_ShouldUpdateCpCorrectly(long newCp)
        {
            // Arrange
            var raiderState = new RaiderState
            {
                Cp = 500L,
            };
            var avatarState = new AvatarState(
                new Address("0x1234567890123456789012345678901234567890"),
                new Address("0x1234567890123456789012345678901234567890"),
                0L,
                new QuestList(new QuestSheet(), new QuestRewardSheet(), new QuestItemRewardSheet(), new EquipmentItemRecipeSheet(), new EquipmentItemSubRecipeSheet()),
                new WorldInformation(0L, null, false),
                new Address("0x1234567890123456789012345678901234567890"),
                "TestAvatar");

            // Act
            raiderState.Update(avatarState, newCp, 1000L, false, 1000L);

            // Assert
            Assert.Equal(newCp, raiderState.Cp);
        }
    }
}
