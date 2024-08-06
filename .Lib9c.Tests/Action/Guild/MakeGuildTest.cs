namespace Lib9c.Tests.Action.Guild
{
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Loader;
    using Nekoyume.Module.Guild;
    using Xunit;

    public class MakeGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var action = new MakeGuild();
            var plainValue = action.PlainValue;

            var actionLoader = new NCActionLoader();
            var loadedRaw = actionLoader.LoadAction(0, plainValue);
            Assert.IsType<MakeGuild>(loadedRaw);
        }

        [Fact]
        public void Execute()
        {
            var guildMasterAddress = AddressUtil.CreateAgentAddress();

            var action = new MakeGuild();
            IWorld world = new World(MockUtil.MockModernWorldState);
            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            });

            var guildAddress = world.GetJoinedGuild(guildMasterAddress);
            Assert.NotNull(guildAddress);
            Assert.True(world.TryGetGuild(guildAddress.Value, out var guild));
            Assert.Equal(guildMasterAddress, guild.GuildMasterAddress);
        }
    }
}
