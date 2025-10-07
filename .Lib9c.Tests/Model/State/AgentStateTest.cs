namespace Lib9c.Tests.Model.State
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Lib9c.Model.State;
    using Libplanet.Crypto;
    using Xunit;

    public class AgentStateTest
    {
        [Fact]
        public void Serialize()
        {
            var agentStateAddress = new PrivateKey().Address;
            var agentState = new AgentState(agentStateAddress);

            var serialized = agentState.SerializeList();
            var deserialized = new AgentState((Bencodex.Types.List)serialized);

            Assert.Equal(agentStateAddress, deserialized.address);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            var agentStateAddress = new PrivateKey().Address;
            var agentState = new AgentState(agentStateAddress);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, agentState);
            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (AgentState)formatter.Deserialize(ms);

            Assert.Equal(agentStateAddress, deserialized.address);
        }
    }
}
