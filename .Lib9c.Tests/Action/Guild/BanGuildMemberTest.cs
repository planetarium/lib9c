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

    public class BanGuildMemberTest
    {
        [Fact]
        public void Serialization()
        {
            var guildMemberAddress = AddressUtil.CreateAgentAddress();
            var action = new BanGuildMember(guildMemberAddress);
            var plainValue = action.PlainValue;

            var actionLoader = new NCActionLoader();
            var loadedRaw = actionLoader.LoadAction(0, plainValue);
            var loadedAction = Assert.IsType<BanGuildMember>(loadedRaw);
            Assert.Equal(guildMemberAddress, loadedAction.Target);
        }

        // Expected use-case.
        [Fact]
        public void Ban_By_GuildMaster()
        {
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var otherGuildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildMemberAddress = AddressUtil.CreateAgentAddress();
            var otherGuildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();
            var otherGuildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = new World(MockWorldState.CreateModern());
            world = world.MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMemberAddress);
            world = world.MakeGuild(otherGuildAddress, otherGuildMasterAddress)
                .JoinGuild(otherGuildAddress, otherGuildMasterAddress)
                .JoinGuild(otherGuildAddress, otherGuildMemberAddress);

            // Guild
            Assert.False(world.IsBanned(guildAddress, guildMasterAddress));
            Assert.Equal(guildAddress, world.GetJoinedGuild(guildMasterAddress));
            Assert.False(world.IsBanned(guildAddress, guildMemberAddress));
            Assert.Equal(guildAddress, world.GetJoinedGuild(guildMemberAddress));
            // Other guild
            Assert.False(world.IsBanned(guildAddress, otherGuildMasterAddress));
            Assert.Equal(otherGuildAddress, world.GetJoinedGuild(otherGuildMasterAddress));
            Assert.False(world.IsBanned(guildAddress, otherGuildMemberAddress));
            Assert.Equal(otherGuildAddress, world.GetJoinedGuild(otherGuildMemberAddress));

            var action = new BanGuildMember(guildMemberAddress);
            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            });

            // Guild
            Assert.False(world.IsBanned(guildAddress, guildMasterAddress));
            Assert.Equal(guildAddress, world.GetJoinedGuild(guildMasterAddress));
            Assert.True(world.IsBanned(guildAddress, guildMemberAddress));
            Assert.Null(world.GetJoinedGuild(guildMemberAddress));
            // Other guild
            Assert.False(world.IsBanned(guildAddress, otherGuildMasterAddress));
            Assert.Equal(otherGuildAddress, world.GetJoinedGuild(otherGuildMasterAddress));
            Assert.False(world.IsBanned(guildAddress, otherGuildMemberAddress));
            Assert.Equal(otherGuildAddress, world.GetJoinedGuild(otherGuildMemberAddress));

            action = new BanGuildMember(otherGuildMasterAddress);
            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            });

            // Guild
            Assert.False(world.IsBanned(guildAddress, guildMasterAddress));
            Assert.Equal(guildAddress, world.GetJoinedGuild(guildMasterAddress));
            Assert.True(world.IsBanned(guildAddress, guildMemberAddress));
            Assert.Null(world.GetJoinedGuild(guildMemberAddress));
            // Other guild
            Assert.True(world.IsBanned(guildAddress, otherGuildMasterAddress));
            Assert.Equal(otherGuildAddress, world.GetJoinedGuild(otherGuildMasterAddress));
            Assert.False(world.IsBanned(guildAddress, otherGuildMemberAddress));
            Assert.Equal(otherGuildAddress, world.GetJoinedGuild(otherGuildMemberAddress));

            action = new BanGuildMember(otherGuildMemberAddress);
            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            });

            // Guild
            Assert.False(world.IsBanned(guildAddress, guildMasterAddress));
            Assert.Equal(guildAddress, world.GetJoinedGuild(guildMasterAddress));
            Assert.True(world.IsBanned(guildAddress, guildMemberAddress));
            Assert.Null(world.GetJoinedGuild(guildMemberAddress));
            // Other guild
            Assert.True(world.IsBanned(guildAddress, otherGuildMasterAddress));
            Assert.Equal(otherGuildAddress, world.GetJoinedGuild(otherGuildMasterAddress));
            Assert.True(world.IsBanned(guildAddress, otherGuildMemberAddress));
            Assert.Equal(otherGuildAddress, world.GetJoinedGuild(otherGuildMemberAddress));

            action = new BanGuildMember(guildMasterAddress);
            // GuildMaster cannot ban itself.
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            }));
        }

        [Fact]
        public void Ban_By_GuildMember()
        {
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildMemberAddress = AddressUtil.CreateAgentAddress();
            var otherAddress = AddressUtil.CreateAgentAddress();
            var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            var action = new BanGuildMember(targetGuildMemberAddress);

            IWorld world = new World(MockWorldState.CreateModern());
            world = world.MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMemberAddress)
                .JoinGuild(guildAddress, targetGuildMemberAddress);

            // GuildMember tries to ban other guild member.
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMemberAddress,
            }));

            // GuildMember tries to ban itself.
            action = new BanGuildMember(guildMemberAddress);
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMemberAddress,
            }));

            action = new BanGuildMember(otherAddress);
            // GuildMember tries to ban other not joined to its guild.
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMemberAddress,
            }));
        }

        [Fact]
        public void Ban_By_Other()
        {
            // NOTE: It assumes 'other' hasn't any guild. If 'other' has its own guild,
            //       it should be assumed as a guild master.
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var otherAddress = AddressUtil.CreateAgentAddress();
            var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = new World(MockWorldState.CreateModern());
            world = world.MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, targetGuildMemberAddress);

            // Other tries to ban GuildMember.
            var action = new BanGuildMember(targetGuildMemberAddress);
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = otherAddress,
            }));

            // Other tries to ban GuildMaster.
            action = new BanGuildMember(guildMasterAddress);
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = otherAddress,
            }));
        }

        [Fact]
        public void RejectGuildApplication()
        {
            var guildAddress = AddressUtil.CreateGuildAddress();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var agentAddress = AddressUtil.CreateAgentAddress();

            IWorld world = new World(MockUtil.MockModernWorldState)
                .MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress)
                .ApplyGuild(agentAddress, guildAddress);
            Assert.True(world.TryGetGuildApplication(agentAddress, out _));

            var action = new BanGuildMember(agentAddress);
            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            });

            Assert.True(world.IsBanned(guildAddress, agentAddress));
            Assert.False(world.TryGetGuildApplication(agentAddress, out _));
        }
    }
}
