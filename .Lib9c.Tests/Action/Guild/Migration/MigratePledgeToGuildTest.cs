namespace Lib9c.Tests.Action.Guild.Migration
{
    using System;
    using Bencodex.Types;
    using Lib9c.Tests.Action;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Guild.Migration;
    using Nekoyume.Action.Loader;
    using Nekoyume.Extensions;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class MigratePledgeToGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var agentAddress = AddressUtil.CreateAgentAddress();
            var action = new MigratePledgeToGuild(agentAddress);
            var plainValue = action.PlainValue;

            var actionLoader = new NCActionLoader();
            var deserialized =
                Assert.IsType<MigratePledgeToGuild>(actionLoader.LoadAction(0, plainValue));

            Assert.Equal(agentAddress, deserialized.Target);
        }

        [Fact]
        public void Execute_When_WithPledgeContract()
        {
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var target = AddressUtil.CreateAgentAddress();
            var caller = AddressUtil.CreateAgentAddress();
            var pledgeAddress = target.GetPledgeAddress();
            var world = new World(MockUtil.MockModernWorldState)
                .MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress)
                .SetLegacyState(pledgeAddress, new List(
                    MeadConfig.PatronAddress.Serialize(),
                    true.Serialize(),
                    RequestPledge.DefaultRefillMead.Serialize()));

            Assert.Null(world.GetJoinedGuild(target));
            var action = new MigratePledgeToGuild(target);

            // Migrate by other.
            var newWorld = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = caller,
            });

            var joinedGuildAddress = Assert.IsType<GuildAddress>(newWorld.GetJoinedGuild(target));
            Assert.True(newWorld.TryGetGuild(joinedGuildAddress, out var guild));
            Assert.Equal(GuildConfig.PlanetariumGuildOwner, guild.GuildMasterAddress);

            // Migrate by itself.
            newWorld = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = target,
            });

            joinedGuildAddress = Assert.IsType<GuildAddress>(newWorld.GetJoinedGuild(target));
            Assert.True(newWorld.TryGetGuild(joinedGuildAddress, out guild));
            Assert.Equal(GuildConfig.PlanetariumGuildOwner, guild.GuildMasterAddress);
        }

        [Fact]
        public void Execute_When_WithUnapprovedPledgeContract()
        {
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var target = AddressUtil.CreateAgentAddress();
            var pledgeAddress = target.GetPledgeAddress();
            var caller = AddressUtil.CreateAgentAddress();
            var world = new World(MockUtil.MockModernWorldState)
                .MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress)
                .SetLegacyState(pledgeAddress, new List(
                    MeadConfig.PatronAddress.Serialize(),
                    false.Serialize(),
                    RequestPledge.DefaultRefillMead.Serialize()));

            Assert.Null(world.GetJoinedGuild(target));
            var action = new MigratePledgeToGuild(target);

            // Migrate by other.
            Assert.Throws<GuildMigrationFailedException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = caller,
            }));

            // Migrate by itself.
            Assert.Throws<GuildMigrationFailedException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = target,
            }));
        }

        [Fact]
        public void Execute_When_WithoutPledgeContract()
        {
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var target = AddressUtil.CreateAgentAddress();
            var caller = AddressUtil.CreateAgentAddress();
            var world = new World(MockUtil.MockModernWorldState)
                .MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress);

            Assert.Null(world.GetJoinedGuild(target));
            var action = new MigratePledgeToGuild(target);

            // Migrate by other.
            Assert.Throws<GuildMigrationFailedException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = caller,
            }));

            // Migrate by itself.
            Assert.Throws<GuildMigrationFailedException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = target,
            }));
        }

        [Fact]
        public void Execute_When_WithoutGuildYet()
        {
            var target = AddressUtil.CreateAgentAddress();
            var caller = AddressUtil.CreateAgentAddress();
            var pledgeAddress = target.GetPledgeAddress();
            var world = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(pledgeAddress, new List(
                    MeadConfig.PatronAddress.Serialize(),
                    true.Serialize(),
                    RequestPledge.DefaultRefillMead.Serialize()));

            Assert.Null(world.GetJoinedGuild(target));
            var action = new MigratePledgeToGuild(target);

            // Migrate by other.
            Assert.Throws<GuildMigrationFailedException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = caller,
            }));

            // Migrate by itself.
            Assert.Throws<GuildMigrationFailedException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = target,
            }));
        }
    }
}
