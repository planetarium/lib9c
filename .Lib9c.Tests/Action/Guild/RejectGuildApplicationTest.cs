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

    public class RejectGuildApplicationTest
    {
        [Fact]
        public void Serialization()
        {
            var agentAddress = AddressUtil.CreateAgentAddress();
            var action = new RejectGuildApplication(agentAddress);
            var plainValue = action.PlainValue;

            var deserialized = new RejectGuildApplication();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(agentAddress, deserialized.Target);
        }

        [Fact]
        public void Execute()
        {
            var appliedMemberAddress = AddressUtil.CreateAgentAddress();
            var nonAppliedMemberAddress = AddressUtil.CreateAgentAddress();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            var world = new World(MockUtil.MockModernWorldState)
                .MakeGuild(guildAddress, guildMasterAddress)
                .ApplyGuild(appliedMemberAddress, guildAddress);

            // These cases should fail because the member didn't apply the guild and
            // non-guild-master-addresses cannot reject the guild application.
            Assert.Throws<InvalidOperationException>(
                () => new RejectGuildApplication(nonAppliedMemberAddress).Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = guildMasterAddress,
                }));
            Assert.Throws<InvalidOperationException>(
                () => new RejectGuildApplication(nonAppliedMemberAddress).Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = appliedMemberAddress,
                }));
            Assert.Throws<InvalidOperationException>(
                () => new RejectGuildApplication(nonAppliedMemberAddress).Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = nonAppliedMemberAddress,
                }));

            // These cases should fail because non-guild-master-addresses cannot reject the guild application.
            Assert.Throws<InvalidOperationException>(
                () => new RejectGuildApplication(appliedMemberAddress).Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = appliedMemberAddress,
                }));
            Assert.Throws<InvalidOperationException>(
                () => new RejectGuildApplication(appliedMemberAddress).Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = nonAppliedMemberAddress,
                }));

            world = new RejectGuildApplication(appliedMemberAddress).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            });

            Assert.False(world.TryGetGuildApplication(appliedMemberAddress, out _));
            Assert.Null(world.GetJoinedGuild(appliedMemberAddress));
        }
    }
}
