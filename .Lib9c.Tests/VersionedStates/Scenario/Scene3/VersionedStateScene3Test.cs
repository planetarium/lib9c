namespace Lib9c.Tests.VersionedStates.Scenario.Scene3
{
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.VersionedStates;
    using Xunit;

    public class VersionedStateScene3Test
    {
        [Fact]
        public void States()
        {
            // Serialize and deserialize with IAccountStateDelta
            IAccountStateDelta states = new State();
            var v2 = new TestStateV2(1);
            var addr = new PrivateKey().ToAddress();

            // Use specific Serialize() method of type.(IVersionedState)
            var serializedV2 = ((IVersionedState)v2).Serialize();
            // Use normal SetState() method.
            states = states.SetState(addr, serializedV2);
            // Use GetVersionedState() method.
            var serializedNext = states.GetState(addr);
            Assert.Equal(serializedV2, serializedNext);
        }
    }
}
