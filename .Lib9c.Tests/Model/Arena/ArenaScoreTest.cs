using Bencodex.Types;
using Lib9c.Model.Arena;
using Libplanet;
using Libplanet.Crypto;
using Xunit;

namespace Lib9c.Tests.Model.Arena
{
    public class ArenaScoreTest
    {
        [Fact]
        public void Serialize()
        {
            var avatarAddress = new PrivateKey().ToAddress();
            var state = new ArenaScore(avatarAddress, 1, 1);
            var serialized = (List)state.Serialize();
            var deserialized = new ArenaScore(serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.Score, deserialized.Score);
        }
    }
}
