namespace Lib9c.Tests.Model.Guild
{
    using System.Threading.Tasks;
    using Lib9c.Tests.Util;
    using Nekoyume.TypedAddress;
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
            var guild = new Nekoyume.Model.Guild.GuildParticipant(
                new AgentAddress("0xB52B7F66B8464986f56053d82F0D80cA412A6F33"),
                new GuildAddress("0xd928ae87311dead490c986c24cc23c37eff892f2"));

            return Verifier.Verify(guild.Bencoded);
        }

        [Fact]
        public void Serialization()
        {
            var guildParticipant = new Nekoyume.Model.Guild.GuildParticipant(
                AddressUtil.CreateAgentAddress(),
                AddressUtil.CreateGuildAddress());
            var newGuildParticipant =
                new Nekoyume.Model.Guild.GuildParticipant(
                    guildParticipant.AgentAddress, guildParticipant.Bencoded);

            Assert.Equal(guildParticipant.GuildAddress, newGuildParticipant.GuildAddress);
        }
    }
}
