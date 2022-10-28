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
            var state = new ArenaInformationV1(avatarAddress, 1, 1);
            var serialized = (List)state.Serialize();
            var deserialized = new ArenaInformationV1(serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.Win, deserialized.Win);
            Assert.Equal(state.Lose, deserialized.Lose);
            Assert.Equal(state.Ticket, deserialized.Ticket);
            Assert.Equal(state.TicketResetCount, deserialized.TicketResetCount);
            Assert.Equal(state.PurchasedTicketCount, deserialized.PurchasedTicketCount);
        }
    }
}
