using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Lib9c.Model.State;
using Libplanet;
using Libplanet.Crypto;
using Xunit;

namespace Lib9c.Tests.Model.State
{
    public class AgentStateTest
    {
        [Fact]
        public void Serialize()
        {
            var agentStateAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentStateAddress);

            var serialized = agentState.Serialize();
            var deserialized = new AgentState((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(agentStateAddress, deserialized.address);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            var agentStateAddress = new PrivateKey().ToAddress();
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
