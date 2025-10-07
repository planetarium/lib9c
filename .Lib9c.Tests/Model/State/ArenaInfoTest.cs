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

    public class ArenaInfoTest
    {
        private TableSheets _tableSheets;

        public ArenaInfoTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Deserialize_FromIntCombatPoint_ShouldDeserializeToLong()
        {
            // Arrange - Simulate old int serialization for CombatPoint
            int oldCombatPoint = 123456789;
            var serialized = Dictionary.Empty
                .Add("avatarAddress", new Address("0x1234567890123456789012345678901234567890").Serialize())
                .Add("agentAddress", new Address("0x1234567890123456789012345678901234567890").Serialize())
                .Add("avatarName", "TestAvatar".Serialize())
                .Add("arenaRecord", Dictionary.Empty
                    .Add("win", 5.Serialize())
                    .Add("lose", 3.Serialize())
                    .Add("draw", 1.Serialize()))
                .Add("level", 50.Serialize())
                .Add("combatPoint", oldCombatPoint.Serialize())
                .Add("armorId", 100.Serialize())
                .Add("active", true)
                .Add("dailyChallengeCount", 3.Serialize())
                .Add("score", 1000.Serialize())
                .Add("receive", false);

            // Act
            var arenaInfo = new ArenaInfo(serialized);

            // Assert
            Assert.Equal((long)oldCombatPoint, arenaInfo.CombatPoint);
        }

        [Theory]
        [InlineData(987654321L)]
        [InlineData(555666777L)]
        [InlineData(-int.MaxValue)]
        [InlineData(999999999999L)]
        public void Deserialize_ShouldDeserializeCorrectly(long combatPoint)
        {
            // Arrange
            var serialized = Dictionary.Empty
                .Add("avatarAddress", new Address("0x1234567890123456789012345678901234567890").Serialize())
                .Add("agentAddress", new Address("0x1234567890123456789012345678901234567890").Serialize())
                .Add("avatarName", "TestAvatar".Serialize())
                .Add("arenaRecord", Dictionary.Empty
                    .Add("win", 5.Serialize())
                    .Add("lose", 3.Serialize())
                    .Add("draw", 1.Serialize()))
                .Add("level", 50.Serialize())
                .Add("combatPoint", combatPoint.Serialize())
                .Add("armorId", 100.Serialize())
                .Add("active", true)
                .Add("dailyChallengeCount", 3.Serialize())
                .Add("score", 1000.Serialize())
                .Add("receive", false);

            // Act
            var arenaInfo = new ArenaInfo(serialized);

            // Assert
            Assert.Equal(combatPoint, arenaInfo.CombatPoint);
        }

        [Fact]
        public void SerializeAndDeserialize_WithMaxIntCombatPoint_ShouldHandleCorrectly()
        {
            // Arrange
            int maxIntValue = int.MaxValue;
            var serialized = Dictionary.Empty
                .Add("avatarAddress", new Address("0x1234567890123456789012345678901234567890").Serialize())
                .Add("agentAddress", new Address("0x1234567890123456789012345678901234567890").Serialize())
                .Add("avatarName", "TestAvatar".Serialize())
                .Add("arenaRecord", Dictionary.Empty
                    .Add("win", 5.Serialize())
                    .Add("lose", 3.Serialize())
                    .Add("draw", 1.Serialize()))
                .Add("level", 50.Serialize())
                .Add("combatPoint", maxIntValue.Serialize())
                .Add("armorId", 100.Serialize())
                .Add("active", true)
                .Add("dailyChallengeCount", 3.Serialize())
                .Add("score", 1000.Serialize())
                .Add("receive", false);

            // Act
            var arenaInfo = new ArenaInfo(serialized);

            // Assert
            Assert.Equal((long)maxIntValue, arenaInfo.CombatPoint);
        }

        [Fact]
        public void Constructor()
        {
            // Arrange & Act
            var address = new Address("0x1234567890123456789012345678901234567890");
            var agentAddress = new Address("0x1234567890123456789012345678901234567890");
            var avatarState = new AvatarState(
                address,
                agentAddress,
                0L,
                new QuestList(
                    new QuestSheet(),
                    new QuestRewardSheet(),
                    new QuestItemRewardSheet(),
                    new EquipmentItemRecipeSheet(),
                    new EquipmentItemSubRecipeSheet()),
                new WorldInformation(0L, null, false),
                address,
                "TestAvatar");
            var characterSheet = _tableSheets.CharacterSheet;
            var arenaInfo = new ArenaInfo(avatarState, characterSheet, true);

            // Assert
            Assert.True(arenaInfo.CombatPoint > 0);
        }
    }
}
