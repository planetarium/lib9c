namespace Lib9c.Tests.Action
{
    using Nekoyume.Action;
    using Xunit;

    public class AgentListTest
    {
        [Fact]
        public void GetAgentList()
        {
            var agentList = AgentList.Addresses;
            Assert.Equal(AgentList.Count, agentList.Count);
        }
    }
}
