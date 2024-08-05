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
                new GuildAddress("0xd928ae87311dead490c986c24cc23c37eff892f2"));

            return Verifier.Verify(guild.Bencoded);
        }

        [Fact]
        public void Serialization()
        {
            var guildParticipant = new Nekoyume.Model.Guild.GuildParticipant(
                AddressUtil.CreateGuildAddress());
            var newGuildParticipant =
                new Nekoyume.Model.Guild.GuildParticipant(guildParticipant.Bencoded);

            Assert.Equal(guildParticipant.GuildAddress, newGuildParticipant.GuildAddress);
        }
    }
}
