namespace Lib9c.Tests.Action.Guild.Migration
{
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Guild.Migration;
    using Nekoyume.Action.Guild.Migration.Controls;
    using Nekoyume.Extensions;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class GuildMigrationCtrlTest
    {
        [Fact]
        public void MigratePlanetariumPledgeToGuild_When_WithUnapprovedPledgeContract()
        {
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var target = AddressUtil.CreateAgentAddress();
            var pledgeAddress = target.GetPledgeAddress();
            var world = new World(MockUtil.MockModernWorldState)
                .MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress)
                .SetLegacyState(pledgeAddress, new List(
                    MeadConfig.PatronAddress.Serialize(),
                    false.Serialize(), // Unapproved
                    RequestPledge.DefaultRefillMead.Serialize()));

            Assert.Null(world.GetJoinedGuild(target));
            Assert.Throws<GuildMigrationFailedException>(() =>
                GuildMigrationCtrl.MigratePlanetariumPledgeToGuild(world, target));
        }

        [Fact]
        public void MigratePlanetariumPledgeToGuild_When_WithPledgeContract()
        {
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var target = AddressUtil.CreateAgentAddress();
            var pledgeAddress = target.GetPledgeAddress();
            var world = new World(MockUtil.MockModernWorldState)
                .MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress)
                .SetLegacyState(pledgeAddress, new List(
                    MeadConfig.PatronAddress.Serialize(),
                    true.Serialize(),
                    RequestPledge.DefaultRefillMead.Serialize()));

            Assert.Null(world.GetJoinedGuild(target));
            world = GuildMigrationCtrl.MigratePlanetariumPledgeToGuild(world, target);

            var joinedGuildAddress = Assert.IsType<GuildAddress>(world.GetJoinedGuild(target));
            Assert.True(world.TryGetGuild(joinedGuildAddress, out var guild));
            Assert.Equal(GuildConfig.PlanetariumGuildOwner, guild.GuildMasterAddress);
        }

        [Fact]
        public void MigratePlanetariumPledgeToGuild_When_WithoutPledgeContract()
        {
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var target = AddressUtil.CreateAgentAddress();
            var world = new World(MockUtil.MockModernWorldState)
                .MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress);

            Assert.Null(world.GetJoinedGuild(target));
            Assert.Throws<GuildMigrationFailedException>(() =>
                GuildMigrationCtrl.MigratePlanetariumPledgeToGuild(world, target));
        }

        [Fact]
        public void MigratePlanetariumPledgeToGuild_When_WithoutGuildYet()
        {
            var target = AddressUtil.CreateAgentAddress();
            var pledgeAddress = target.GetPledgeAddress();
            var world = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(pledgeAddress, new List(
                    MeadConfig.PatronAddress.Serialize(),
                    true.Serialize(),
                    RequestPledge.DefaultRefillMead.Serialize()));

            Assert.Null(world.GetJoinedGuild(target));
            Assert.Throws<GuildMigrationFailedException>(() =>
                GuildMigrationCtrl.MigratePlanetariumPledgeToGuild(world, target));
        }
    }
}
