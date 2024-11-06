namespace Lib9c.Tests.Action.Guild
{
    using System;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume.Action.Guild;
    using Nekoyume.Module.Guild;
    using Xunit;

    public class UnbanGuildMemberTest
    {
        [Fact]
        public void Serialization()
        {
            var guildMemberAddress = new PrivateKey().Address;
            var action = new UnbanGuildMember(guildMemberAddress);
            var plainValue = action.PlainValue;

            var deserialized = new UnbanGuildMember();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(guildMemberAddress, deserialized.Target);
        }

        [Fact]
        public void Unban_By_GuildMember()
        {
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildMemberAddress = AddressUtil.CreateAgentAddress();
            var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            var action = new UnbanGuildMember(targetGuildMemberAddress);

            IWorld world = new World(MockWorldState.CreateModern());
            world = world.MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMemberAddress)
                .JoinGuild(guildAddress, targetGuildMemberAddress);

            // GuildMember tries to ban other guild member.
            Assert.Throws<InvalidOperationException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Signer = guildMemberAddress,
                    }));

            // GuildMember tries to ban itself.
            Assert.Throws<InvalidOperationException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Signer = targetGuildMemberAddress,
                    }));
        }

        [Fact]
        public void Unban_By_GuildMaster()
        {
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            var action = new UnbanGuildMember(targetGuildMemberAddress);

            IWorld world = new World(MockWorldState.CreateModern());
            world = world.MakeGuild(guildAddress, guildMasterAddress)
                .Ban(guildAddress, guildMasterAddress, targetGuildMemberAddress);

            Assert.True(world.IsBanned(guildAddress, targetGuildMemberAddress));
            Assert.Null(world.GetJoinedGuild(targetGuildMemberAddress));

            world = action.Execute(
                new ActionContext
                {
                    PreviousState = world,
                    Signer = guildMasterAddress,
                });

            Assert.False(world.IsBanned(guildAddress, targetGuildMemberAddress));
            Assert.Null(world.GetJoinedGuild(targetGuildMemberAddress));
        }
    }
}
