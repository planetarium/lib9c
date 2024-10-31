namespace Lib9c.Tests.Action.Guild.Migration
{
    using System;
    using System.Linq;
    using System.Numerics;
    using Bencodex.Types;
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
    public class MigrateDelegationTest : GuildTestBase
    {
        [Fact]
        public void Execute()
        {
            var guildAddress = AddressUtil.CreateGuildAddress();
            var world = EnsureLegacyPlanetariumGuild(World, guildAddress);
            var guildMemberCount = 10;
            var migratedGuildMemberCount = 5;
            var guildMemberAddresses = Enumerable.Range(0, guildMemberCount).Select(
                _ => AddressUtil.CreateAgentAddress()).ToList();
            for (var i = 0; i < guildMemberCount; i++)
            {
                world = EnsureJoinLegacyPlanetariumGuild(world, guildMemberAddresses[i]);
            }

            world = EnsureMigratedPlanetariumGuild(world);

            for (var i = 0; i < migratedGuildMemberCount; i++)
            {
                var action = new MigrateDelegation(guildMemberAddresses[i]);
                var actionContext = new ActionContext
                {
                    PreviousState = world,
                    Signer = new PrivateKey().Address,
                };
                world = action.Execute(actionContext);
            }

            var repo = new GuildRepository(world, new ActionContext());
            var guild = repo.GetGuild(guildAddress);

            for (var i = 0; i < migratedGuildMemberCount; i++)
            {
                repo.GetGuildParticipant(guildMemberAddresses[i]);
            }

            for (var i = migratedGuildMemberCount; i < guildMemberCount; i++)
            {
                Assert.Throws<FailedLoadStateException>(() => repo.GetGuildParticipant(guildMemberAddresses[i]));
            }
        }

        private static IWorld EnsureLegacyPlanetariumGuild(IWorld world, GuildAddress guildAddress)
        {
            var legacyPlanetariumGuild = new LegacyGuild(GuildConfig.PlanetariumGuildOwner);
            var legacyPlanetariumGuildParticipant = new LegacyGuildParticipant(guildAddress);

            return world
                .MutateAccount(
                    Addresses.Guild,
                    account => account.SetState(guildAddress, legacyPlanetariumGuild.Bencoded))
                .MutateAccount(
                    Addresses.GuildParticipant,
                    account => account.SetState(GuildConfig.PlanetariumGuildOwner, legacyPlanetariumGuildParticipant.Bencoded))
                .MutateAccount(
                    Addresses.GuildMemberCounter,
                    account =>
                    {
                        BigInteger count = account.GetState(guildAddress) switch
                        {
                            Integer i => i.Value,
                            null => 0,
                            _ => throw new InvalidCastException(),
                        };

                        return account.SetState(guildAddress, (Integer)(count + 1));
                    });
        }

        private static IWorld EnsureJoinLegacyPlanetariumGuild(IWorld world, AgentAddress guildParticipantAddress)
        {
            var planetariumGuildAddress
                = new LegacyGuildParticipant(
                    world.GetAccount(Addresses.GuildParticipant).GetState(GuildConfig.PlanetariumGuildOwner) as List).GuildAddress;
            var legacyParticipant = new LegacyGuildParticipant(planetariumGuildAddress);

            return world
                .MutateAccount(Addresses.GuildParticipant, account => account.SetState(guildParticipantAddress, legacyParticipant.Bencoded))
                .MutateAccount(
                    Addresses.GuildMemberCounter,
                    account =>
                    {
                        BigInteger count = account.GetState(planetariumGuildAddress) switch
                        {
                            Integer i => i.Value,
                            null => 0,
                            _ => throw new InvalidCastException(),
                        };

                        return account.SetState(planetariumGuildAddress, (Integer)(count + 1));
                    });
        }

        private static IWorld EnsureMigratedPlanetariumGuild(IWorld world)
            => new MigratePlanetariumGuild().Execute(new ActionContext
            {
                PreviousState = world,
                Signer = new PrivateKey().Address,
            });
    }
}
