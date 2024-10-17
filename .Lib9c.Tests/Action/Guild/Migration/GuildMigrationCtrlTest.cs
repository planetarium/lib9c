namespace Lib9c.Tests.Action.Guild.Migration
{
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Guild.Migration;
    using Nekoyume.Action.Guild.Migration.Controls;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Guild;
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
            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);
            repository.JoinGuild(guildAddress, guildMasterAddress);
            repository.UpdateWorld(repository.World.SetLegacyState(pledgeAddress, new List(
                MeadConfig.PatronAddress.Serialize(),
                false.Serialize(),  // Unapproved
                RequestPledge.DefaultRefillMead.Serialize())));

            Assert.Null(repository.GetJoinedGuild(target));
            Assert.Throws<GuildMigrationFailedException>(() =>
                GuildMigrationCtrl.MigratePlanetariumPledgeToGuild(repository, target));
        }

        [Fact]
        public void MigratePlanetariumPledgeToGuild_When_WithPledgeContract()
        {
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var target = AddressUtil.CreateAgentAddress();
            var pledgeAddress = target.GetPledgeAddress();
            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);
            repository.JoinGuild(guildAddress, guildMasterAddress);
            repository.UpdateWorld(repository.World.SetLegacyState(pledgeAddress, new List(
                MeadConfig.PatronAddress.Serialize(),
                true.Serialize(),
                RequestPledge.DefaultRefillMead.Serialize())));

            Assert.Null(repository.GetJoinedGuild(target));
            GuildMigrationCtrl.MigratePlanetariumPledgeToGuild(repository, target);

            var joinedGuildAddress = Assert.IsType<GuildAddress>(repository.GetJoinedGuild(target));
            Assert.True(repository.TryGetGuild(joinedGuildAddress, out var guild));
            Assert.Equal(GuildConfig.PlanetariumGuildOwner, guild.GuildMasterAddress);
        }

        [Fact]
        public void MigratePlanetariumPledgeToGuild_When_WithoutPledgeContract()
        {
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var target = AddressUtil.CreateAgentAddress();
            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);
            repository.JoinGuild(guildAddress, guildMasterAddress);

            Assert.Null(repository.GetJoinedGuild(target));
            Assert.Throws<GuildMigrationFailedException>(() =>
                GuildMigrationCtrl.MigratePlanetariumPledgeToGuild(repository, target));
        }

        [Fact]
        public void MigratePlanetariumPledgeToGuild_When_WithoutGuildYet()
        {
            var target = AddressUtil.CreateAgentAddress();
            var pledgeAddress = target.GetPledgeAddress();
            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetLegacyState(pledgeAddress, new List(
                    MeadConfig.PatronAddress.Serialize(),
                    true.Serialize(),
                    RequestPledge.DefaultRefillMead.Serialize()));
            var repository = new GuildRepository(world, new ActionContext());
            Assert.Null(repository.GetJoinedGuild(target));
            Assert.Throws<GuildMigrationFailedException>(() =>
                GuildMigrationCtrl.MigratePlanetariumPledgeToGuild(repository, target));
        }
    }
}
