namespace Lib9c.Tests.Action.Guild
{
    using System;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Loader;
    using Nekoyume.Module.Guild;
    using Xunit;

    public class ApplyGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var guildAddress = AddressUtil.CreateGuildAddress();
            var action = new ApplyGuild(guildAddress);
            var plainValue = action.PlainValue;

            var actionLoader = new NCActionLoader();
            var loadedRaw = actionLoader.LoadAction(0, plainValue);
            var loadedAction = Assert.IsType<ApplyGuild>(loadedRaw);
            Assert.Equal(guildAddress, loadedAction.GuildAddress);
        }

        [Fact]
        public void Execute()
        {
            var agentAddress = AddressUtil.CreateAgentAddress();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();
            var action = new ApplyGuild(guildAddress);

            IWorld world = new World(MockUtil.MockModernWorldState)
                .MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress);
            IWorld bannedWorld = world.Ban(guildAddress, agentAddress);

            // This case should fail because the agent is banned by the guild.
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = bannedWorld,
                Signer = agentAddress,
            }));

            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = agentAddress,
            });

            Assert.True(world.TryGetGuildApplication(agentAddress, out var application));
            Assert.Equal(guildAddress, application.GuildAddress);
        }
    }
}
