namespace Lib9c.Tests.Model.Guild
{
    using System.Threading.Tasks;
    using Lib9c.Model.Guild;
    using Lib9c.Tests.Action;
    using Lib9c.Tests.Util;
    using Lib9c.TypedAddress;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using VerifyTests;
    using VerifyXunit;
    using Xunit;

    [UsesVerify]
    public class GuildParticipantTest
    {
        public GuildParticipantTest()
        {
            VerifierSettings.SortPropertiesAlphabetically();
        }

        [Fact]
        public Task Snapshot()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);
            var repository = new GuildRepository(world, new ActionContext());
            var guild = new GuildParticipant(
                new AgentAddress("0xB52B7F66B8464986f56053d82F0D80cA412A6F33"),
                new GuildAddress("0xd928ae87311dead490c986c24cc23c37eff892f2"),
                repository);

            return Verifier.Verify(guild.Bencoded);
        }

        [Fact]
        public void Serialization()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);
            var repository = new GuildRepository(world, new ActionContext());
            var guildParticipant = new GuildParticipant(
                AddressUtil.CreateAgentAddress(),
                AddressUtil.CreateGuildAddress(),
                repository);
            repository.SetGuildParticipant(guildParticipant);
            var newGuildParticipant =
                new GuildParticipant(
                    guildParticipant.Address, guildParticipant.Bencoded, repository);

            Assert.Equal(guildParticipant.GuildAddress, newGuildParticipant.GuildAddress);
        }
    }
}
