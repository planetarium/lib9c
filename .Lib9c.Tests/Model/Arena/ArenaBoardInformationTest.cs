namespace Lib9c.Tests.Model.Arena
{
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.Arena;
    using Xunit;

    public class ArenaBoardInformationTest
    {
        [Fact]
        public void Serialize()
        {
            var avatarAddress = new PrivateKey().ToAddress();
            var state = new ArenaBoardInformation(avatarAddress, 1, 1);
            var serialized = (List)state.Serialize();
            var deserialized = new ArenaBoardInformation(serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.NameWithHash, deserialized.NameWithHash);
            Assert.Equal(state.PortraitId, deserialized.PortraitId);
            Assert.Equal(state.Level, deserialized.Level);
            Assert.Equal(state.CP, deserialized.CP);
        }
    }
}
