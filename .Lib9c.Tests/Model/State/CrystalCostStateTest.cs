namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Lib9c.Helper;
    using Lib9c.Model.State;
    using Libplanet.Crypto;
    using Xunit;

    public class CrystalCostStateTest
    {
        [Fact]
        public void Serialize()
        {
            var crystal = 100 * CrystalCalculator.CRYSTAL;
            var address = new PrivateKey().Address;
            var state = new CrystalCostState(address, crystal);
            state.Count++;
            var serialized = state.Serialize();
            var deserialized = new CrystalCostState(address, (List)serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.CRYSTAL, deserialized.CRYSTAL);
            Assert.Equal(state.Count, deserialized.Count);
        }
    }
}
