namespace Lib9c.Tests.Action.Guild.Migration
{
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Guild.Migration;
    using Nekoyume.Action.Guild.Migration.LegacyModels;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Guild;
    using Nekoyume.TypedAddress;
    using Xunit;

    // TODO: Remove this test class after the migration is completed.
    public class MigratePlanetariumGuildTest : GuildTestBase
    {
        [Fact]
        public void Execute()
        {
            var guildAddress = AddressUtil.CreateGuildAddress();
            var world = EnsureLegacyPlanetariumGuild(World, guildAddress);

            var action = new MigratePlanetariumGuild();
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = new PrivateKey().Address,
            };
            world = action.Execute(actionContext);

            var repo = new GuildRepository(world, new ActionContext());

            var guild = repo.GetGuild(guildAddress);
            Assert.Throws<FailedLoadStateException>(() => repo.GetGuildParticipant(GuildConfig.PlanetariumGuildOwner));
        }

        private static IWorld EnsureLegacyPlanetariumGuild(IWorld world, GuildAddress guildAddress)
        {
            var legacyPlanetariumGuild = new LegacyGuild(GuildConfig.PlanetariumGuildOwner);
            var legacyPlanetariumGuildParticipant = new LegacyGuildParticipant(guildAddress);
            var guildAccount = world.GetAccount(Addresses.Guild);
            var guildParticipantAccount = world.GetAccount(Addresses.GuildParticipant);
            return world
                .MutateAccount(
                    Addresses.Guild,
                    account => account.SetState(guildAddress, legacyPlanetariumGuild.Bencoded))
                .MutateAccount(
                    Addresses.GuildParticipant,
                    account => account.SetState(GuildConfig.PlanetariumGuildOwner, legacyPlanetariumGuildParticipant.Bencoded));
        }

        private static IWorld EnsureMigratedPlanetariumGuild(IWorld world)
            => new MigratePlanetariumGuild().Execute(new ActionContext
            {
                PreviousState = world,
                Signer = new PrivateKey().Address,
            });
    }
}
