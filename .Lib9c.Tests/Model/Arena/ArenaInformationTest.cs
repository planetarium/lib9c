using Bencodex.Types;
using Lib9c.Model.Arena;
using Libplanet;
using Libplanet.Crypto;
using Xunit;

namespace Lib9c.Tests.Model.Arena
{
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
    }
}
