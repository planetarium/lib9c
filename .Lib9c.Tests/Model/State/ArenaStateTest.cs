namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Nekoyume.Model.State;
    using Xunit;

    public class ArenaStateTest
    {
        [Fact]
        public void Serialize()
        {
            var state = new ArenaState(0);
            var serialized = (List)state.Serialize();
            var deserialized = new ArenaState(serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.AvatarAddresses, deserialized.AvatarAddresses);
        }
    }
}
