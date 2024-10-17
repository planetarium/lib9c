namespace Lib9c.Tests.Model.Guild
{
    using System.Threading.Tasks;
    using Lib9c.Tests.Action;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume.TypedAddress;
    using VerifyTests;
    using VerifyXunit;
    using Xunit;

    [UsesVerify]
    public class GuildTest
    {
        public GuildTest()
        {
            VerifierSettings.SortPropertiesAlphabetically();
        }

        [Fact]
        public Task Snapshot()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);
            var repository = new Nekoyume.Model.Guild.GuildRepository(world, new ActionContext());
            var validatorAddress = new Address("0x614bBf3cE78657b6E1673cA77997adDf510538Df");
            var guild = new Nekoyume.Model.Guild.Guild(
                AddressUtil.CreateGuildAddress(),
                new AgentAddress("0xd928ae87311dead490c986c24cc23c37eff892f2"),
                validatorAddress,
                Currency.Legacy("NCG", 2, null),
                repository);

            return Verifier.Verify(guild.Bencoded);
        }

        [Fact]
        public void Serialization()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);
            var repository = new Nekoyume.Model.Guild.GuildRepository(world, new ActionContext());
            var guildAddress = AddressUtil.CreateGuildAddress();
            var validatorAddress = new PrivateKey().Address;
            var guild = new Nekoyume.Model.Guild.Guild(
                guildAddress,
                AddressUtil.CreateAgentAddress(),
                validatorAddress,
                Currency.Legacy("NCG", 2, null),
                repository);
            repository.SetGuild(guild);
            var newGuild = new Nekoyume.Model.Guild.Guild(
                guildAddress,
                guild.Bencoded,
                repository);

            Assert.Equal(guild.GuildMasterAddress, newGuild.GuildMasterAddress);
        }
    }
}
