namespace Lib9c.Tests.Action.Guild
{
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class AdminMakeGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var action = new AdminMakeGuild();
            var plainValue = action.PlainValue;

            var deserialized = new AdminMakeGuild();
            deserialized.LoadPlainValue(plainValue);
        }

        [Fact]
        public void Execute()
        {
            var adminAddress = AddressUtil.CreateAgentAddress();
            var world = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(
                    AdminState.Address,
                    new AdminState(adminAddress, 100).Serialize());
            var action = new AdminMakeGuild();
            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = adminAddress,
            });

            var joinedGuildAddress =
                Assert.IsType<GuildAddress>(
                    world.GetJoinedGuild(new AgentAddress(MeadConfig.PatronAddress)));
            Assert.True(world.TryGetGuild(joinedGuildAddress, out var guild));
            Assert.Equal(MeadConfig.PatronAddress, guild.GuildMasterAddress);
        }

        [Fact]
        public void Execute_By_NonAdmin()
        {
            var signer = AddressUtil.CreateAgentAddress();
            var adminAddress = AddressUtil.CreateAgentAddress();
            var world = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(AdminState.Address, new AdminState(adminAddress, 100).Serialize());
            var action = new AdminMakeGuild();
            Assert.Throws<PermissionDeniedException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = signer,
            }));
        }
    }
}
