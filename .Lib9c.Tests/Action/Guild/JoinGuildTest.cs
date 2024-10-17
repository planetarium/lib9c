namespace Lib9c.Tests.Action.Guild
{
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action.Guild;
    using Nekoyume.Model.Guild;
    using Nekoyume.Module.Guild;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class JoinGuildTest : GuildTestBase
    {
        [Fact]
        public void Serialization()
        {
            var guildAddress = AddressUtil.CreateGuildAddress();
            var action = new JoinGuild(guildAddress);
            var plainValue = action.PlainValue;

            var deserialized = new JoinGuild();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(guildAddress, deserialized.GuildAddress);
        }

        [Fact]
        public void Execute()
        {
            var validatorKey = new PrivateKey();
            var agentAddress = AddressUtil.CreateAgentAddress();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);

            var repository = new GuildRepository(world, new ActionContext
            {
                Signer = guildMasterAddress,
            });
            repository.MakeGuild(guildAddress, guildMasterAddress);
            repository.JoinGuild(guildAddress, agentAddress);

            var guildParticipant = repository.GetGuildParticipant(agentAddress);

            Assert.Equal(agentAddress, guildParticipant.Address);
        }
    }
}
