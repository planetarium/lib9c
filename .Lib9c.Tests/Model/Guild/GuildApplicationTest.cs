namespace Lib9c.Tests.Model.Guild
{
    using System.Threading.Tasks;
    using Lib9c.Tests.Util;
    using Nekoyume.TypedAddress;
    using VerifyTests;
    using VerifyXunit;
    using Xunit;

    [UsesVerify]
    public class GuildApplicationTest
    {
        public GuildApplicationTest()
        {
            VerifierSettings.SortPropertiesAlphabetically();
        }

        [Fact]
        public Task Snapshot()
        {
            var guildApplication = new Nekoyume.Model.Guild.GuildApplication(
                new GuildAddress("0xd928ae87311dead490c986c24cc23c37eff892f2"));

            return Verifier.Verify(guildApplication.Bencoded);
        }

        [Fact]
        public void Serialization()
        {
            var guildApplication = new Nekoyume.Model.Guild.GuildApplication(
                AddressUtil.CreateGuildAddress());
            var newGuildApplication =
                new Nekoyume.Model.Guild.GuildApplication(guildApplication.Bencoded);

            Assert.Equal(guildApplication.GuildAddress, newGuildApplication.GuildAddress);
        }
    }
}
