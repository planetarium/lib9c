namespace Lib9c.Tests.Action.Guild
{
    using System;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Loader;
    using Nekoyume.Module.Guild;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class RemoveGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var action = new RemoveGuild();
            var plainValue = action.PlainValue;

            var actionLoader = new NCActionLoader();
            var loadedRaw = actionLoader.LoadAction(0, plainValue);
            Assert.IsType<RemoveGuild>(loadedRaw);
        }

        [Fact]
        public void Execute_By_GuildMember()
        {
            var action = new RemoveGuild();

            PrivateKey guildMaster = new (), guildMember = new ();
            var guildMasterAddress = new AgentAddress(guildMaster.Address);
            var guildMemberAddress = new AgentAddress(guildMember.Address);
            var guildAddress = new GuildAddress(guildMasterAddress);

            IWorld world = new World(MockWorldState.CreateModern());
            world = world.MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMemberAddress);

            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMemberAddress,
            }));
        }

        [Fact]
        public void Execute_By_GuildMaster()
        {
            var action = new RemoveGuild();

            PrivateKey guildMaster = new ();
            var guildMasterAddress = new AgentAddress(guildMaster.Address);
            var guildAddress = new GuildAddress(guildMasterAddress);

            IWorld world = new World(MockWorldState.CreateModern());
            world = world.MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress);

            var changedWorld = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            });

            Assert.False(changedWorld.TryGetGuild(guildAddress, out _));
            Assert.Null(changedWorld.GetJoinedGuild(guildMasterAddress));
        }

        [Fact]
        public void Execute_By_Other()
        {
            var action = new RemoveGuild();

            PrivateKey guildMaster = new (), other = new ();
            var guildMasterAddress = new AgentAddress(guildMaster.Address);
            var otherAddress = new AgentAddress(other.Address);
            var guildAddress = new GuildAddress(guildMasterAddress);

            IWorld world = new World(MockWorldState.CreateModern());
            world = world.MakeGuild(guildAddress, guildMasterAddress);

            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = otherAddress,
            }));
        }
    }
}
