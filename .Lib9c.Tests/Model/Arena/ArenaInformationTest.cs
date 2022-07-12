namespace Lib9c.Tests.Model.Arena
{
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.Arena;
    using Xunit;

    public class ArenaInformationTest
    {
        [Fact]
        public void Serialize()
        {
            var avatarAddress = new PrivateKey().ToAddress();
            var state = new ArenaInformation(avatarAddress, 1, 1);
            var serialized = (List)state.Serialize();
            var deserialized = new ArenaInformation(serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.Win, deserialized.Win);
            Assert.Equal(state.Lose, deserialized.Lose);
            Assert.Equal(state.Ticket, deserialized.Ticket);
            Assert.Equal(state.TicketResetCount, deserialized.TicketResetCount);
            Assert.Equal(state.PurchasedTicketCount, deserialized.PurchasedTicketCount);
        }

        [Theory]
        [InlineData(999L, 500, 5, 4)]
        [InlineData(1_000L, 500, 5, 5)]
        [InlineData(1_199L, 500, 5, 5)]
        [InlineData(1_200L, 500, 5, 6)]
        public void BuyTicket(
            long roundBlockRange,
            int dailyArenaInterval,
            int dailyArenaTicketCount,
            int expectedMax)
        {
            var ai = new ArenaInformation(new PrivateKey().ToAddress(), 1, 1);
            for (var i = 0; i < expectedMax; i++)
            {
                ai.BuyTicket(
                    roundBlockRange,
                    dailyArenaInterval,
                    dailyArenaTicketCount);

                Assert.Equal(i + 1, ai.PurchasedTicketCount);
            }
        }

        [Theory]
        [InlineData(999L, 500, 5, 4)]
        [InlineData(1_000L, 500, 5, 5)]
        [InlineData(1_199L, 500, 5, 5)]
        [InlineData(1_200L, 500, 5, 6)]
        public void BuyTicket_Throw_ExceedTicketPurchaseLimitException(
            long roundBlockRange,
            int dailyArenaInterval,
            int dailyArenaTicketCount,
            int expectedMax)
        {
            Assert.Throws<ExceedTicketPurchaseLimitException>(() =>
            {
                BuyTicket(
                    roundBlockRange,
                    dailyArenaInterval,
                    dailyArenaTicketCount,
                    expectedMax + 1);
            });
        }
    }
}
