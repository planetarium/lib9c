namespace Lib9c.Tests.Action.Guild.Migration
{
    using System;
    using Bencodex.Types;
    using Lib9c.Tests.Action;
    using Lib9c.Tests.Model.Guild;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Guild.Migration;
    using Nekoyume.Action.Loader;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Guild;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class MigratePledgeToGuildTest : GuildTestBase
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
            var validatorKey = new PrivateKey();
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var target = AddressUtil.CreateAgentAddress();
            var caller = AddressUtil.CreateAgentAddress();
            var pledgeAddress = target.GetPledgeAddress();
            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

            var repository = new GuildRepository(world, new ActionContext());
            repository.UpdateWorld(repository.World.SetLegacyState(
                pledgeAddress,
                new List(
                    MeadConfig.PatronAddress.Serialize(),
                    true.Serialize(),
                    RequestPledge.DefaultRefillMead.Serialize())));

            Assert.Null(repository.GetJoinedGuild(target));
            var action = new MigratePledgeToGuild(target);

            // Migrate by other.
            IWorld newWorld = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = caller,
            });

            var newRepository = new GuildRepository(newWorld, new ActionContext());
            var joinedGuildAddress = Assert.IsType<GuildAddress>(newRepository.GetJoinedGuild(target));
            Assert.True(newRepository.TryGetGuild(joinedGuildAddress, out var guild));
            Assert.Equal(GuildConfig.PlanetariumGuildOwner, guild.GuildMasterAddress);

            // Migrate by itself.
            newWorld = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = target,
            });

            newRepository.UpdateWorld(newWorld);
            joinedGuildAddress = Assert.IsType<GuildAddress>(newRepository.GetJoinedGuild(target));
            Assert.True(newRepository.TryGetGuild(joinedGuildAddress, out guild));
            Assert.Equal(GuildConfig.PlanetariumGuildOwner, guild.GuildMasterAddress);
        }

        [Fact]
        public void Execute_When_WithUnapprovedPledgeContract()
        {
            var validatorKey = new PrivateKey();
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var target = AddressUtil.CreateAgentAddress();
            var pledgeAddress = target.GetPledgeAddress();
            var caller = AddressUtil.CreateAgentAddress();
            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

            var repository = new GuildRepository(world, new ActionContext());
            repository.UpdateWorld(repository.World.SetLegacyState(pledgeAddress, new List(
                MeadConfig.PatronAddress.Serialize(),
                false.Serialize(),
                RequestPledge.DefaultRefillMead.Serialize())));

            Assert.Null(repository.GetJoinedGuild(target));
            var action = new MigratePledgeToGuild(target);

            // Migrate by other.
            Assert.Throws<GuildMigrationFailedException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = caller,
            }));

            // Migrate by itself.
            Assert.Throws<GuildMigrationFailedException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = target,
            }));
        }

        [Fact]
        public void Execute_When_WithoutPledgeContract()
        {
            var validatorKey = new PrivateKey();
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var target = AddressUtil.CreateAgentAddress();
            var caller = AddressUtil.CreateAgentAddress();
            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

            var repository = new GuildRepository(world, new ActionContext());

            Assert.Null(repository.GetJoinedGuild(target));
            var action = new MigratePledgeToGuild(target);

            // Migrate by other.
            Assert.Throws<GuildMigrationFailedException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = caller,
            }));

            // Migrate by itself.
            Assert.Throws<GuildMigrationFailedException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
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
            var repository = new GuildRepository(world, new ActionContext());
            Assert.Null(repository.GetJoinedGuild(target));
            var action = new MigratePledgeToGuild(target);

            // Migrate by other.
            Assert.Throws<GuildMigrationFailedException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = caller,
            }));

            // Migrate by itself.
            Assert.Throws<GuildMigrationFailedException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = target,
            }));
        }
    }
}
