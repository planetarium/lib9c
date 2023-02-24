namespace Lib9c.Tests.VersionedStates.Scenario.Scene2
{
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.VersionedStates;
    using Xunit;

    public class VersionedStateScene2Test
    {
        [Fact]
        public void States()
        {
            // Serialize and deserialize with IAccountStateDelta
            IAccountStateDelta states = new State();
            var state = new TestState(1);
            var addr = new PrivateKey().ToAddress();

            // Use specific Serialize() method of type.(INonVersionedState)
            var serializedNonVersioned = ((INonVersionedState)state).Serialize();
            // Use normal SetState() method.
            states = states.SetState(addr, serializedNonVersioned);
            // Use GetVersionedState() method.
            var serializedNext = states.GetState(addr);
            Assert.Equal(serializedNonVersioned, serializedNext);

            // Use specific Serialize() method of type.(IVersionedState)
            var serializedV1 = ((IVersionedState)state).Serialize();
            // Use normal SetState() method.
            states = states.SetState(addr, serializedV1);
            // Use GetVersionedState() method.
            serializedNext = states.GetState(addr);
            Assert.Equal(serializedV1, serializedNext);
        }
    }
}
