using Bencodex.Types;
using Lib9c.Model.Arena;
using Xunit;

namespace Lib9c.Tests.Model.Arena
{
    public class ArenaParticipantsTest
    {
        [Fact]
        public void Serialize()
        {
            var state = new ArenaParticipants(1, 1);
            var serialized = (List)state.Serialize();
            var deserialized = new ArenaParticipants(serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.AvatarAddresses, deserialized.AvatarAddresses);
        }
    }
}
