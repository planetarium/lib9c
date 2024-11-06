namespace Lib9c.Tests.Model.Arena
{
    using Libplanet.Crypto;
    using Nekoyume.Model.Arena;
    using Xunit;

    public class ArenaParticipantTest
    {
        [Fact]
        public void Serialize()
        {
            var avatarAddr = new PrivateKey().Address;
            var state = new ArenaParticipant(avatarAddr)
            {
                Name = "Joy",
                PortraitId = 1,
                Level = 99,
                Cp = 999_999_999,
                Score = 999_999,
                Ticket = 7,
                TicketResetCount = 1,
                PurchasedTicketCount = 1,
                Win = 100,
                Lose = 99,
                LastBattleBlockIndex = long.MaxValue,
            };
            var serialized = state.Bencoded;
            var deserialized = new ArenaParticipant(serialized);

            Assert.Equal(state.AvatarAddr, deserialized.AvatarAddr);
            Assert.Equal(state.Name, deserialized.Name);
            Assert.Equal(state.PortraitId, deserialized.PortraitId);
            Assert.Equal(state.Level, deserialized.Level);
            Assert.Equal(state.Cp, deserialized.Cp);
            Assert.Equal(state.Score, deserialized.Score);
            Assert.Equal(state.Ticket, deserialized.Ticket);
            Assert.Equal(state.TicketResetCount, deserialized.TicketResetCount);
            Assert.Equal(state.PurchasedTicketCount, deserialized.PurchasedTicketCount);
            Assert.Equal(state.Win, deserialized.Win);
            Assert.Equal(state.Lose, deserialized.Lose);
            Assert.Equal(state.LastBattleBlockIndex, deserialized.LastBattleBlockIndex);

            var serialized2 = deserialized.Bencoded;
            Assert.Equal(serialized, serialized2);
        }
    }
}
