namespace Lib9c.Tests.VersionedStates.Scenario.Scene1
{
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Xunit;

    public class VersionedStateScene1Test
    {
        [Fact]
        public void Test()
        {
            // serialize and deserialize
            var state = new TestState(1);
            Assert.Equal(1, state.Value);
            var serialized = state.Serialize();
            var deserialized = new TestState(serialized);
            Assert.Equal(state.Value, deserialized.Value);
            Assert.Equal(serialized, deserialized.Serialize());

            // serialize and deserialize with IAccountStateDelta
            IAccountStateDelta states = new State();
            var addr = new PrivateKey().ToAddress();
            states = states.SetState(addr, serialized);
            var serializedNext = states.GetState(addr);
            Assert.Equal(serialized, serializedNext);
        }
    }
}
