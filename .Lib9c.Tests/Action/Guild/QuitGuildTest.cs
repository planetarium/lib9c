namespace Lib9c.Tests.Action.Guild
{
    using System;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume.Action.Guild;
    using Nekoyume.Module.Guild;
    using Xunit;

    public class QuitGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var action = new QuitGuild();
            var plainValue = action.PlainValue;

            var deserialized = new QuitGuild();
            deserialized.LoadPlainValue(plainValue);
        }

        [Fact]
        public void Execute()
        {
            var agentAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();

            var action = new QuitGuild();
            var world = new World(MockUtil.MockModernWorldState)
                .MakeGuild(guildAddress, guildMasterAddress);

            // This case should fail because guild master cannot quit the guild.
            Assert.Throws<InvalidOperationException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Signer = guildMasterAddress,
                    }));

            // This case should fail because the agent is not a member of the guild.
            Assert.Throws<InvalidOperationException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Signer = agentAddress,
                    }));

            // Join the guild.
            world = world.JoinGuild(guildAddress, agentAddress);
            Assert.NotNull(world.GetJoinedGuild(agentAddress));

            // This case should fail because the agent is not a member of the guild.
            world = action.Execute(
                new ActionContext
                {
                    PreviousState = world,
                    Signer = agentAddress,
                });

            Assert.Null(world.GetJoinedGuild(agentAddress));
        }
    }
}
