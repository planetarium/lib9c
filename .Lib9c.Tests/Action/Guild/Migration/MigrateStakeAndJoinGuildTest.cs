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
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class MigrateStakeAndJoinGuildTest : GuildTestBase
    {
        [Fact]
        public void Execute()
        {
            var guildAddress = AddressUtil.CreateGuildAddress();
            var validatorKey = new PrivateKey();
            var adminAddress = new PrivateKey().Address;
            var height = 100L;
            var world = World.SetLegacyState(Addresses.Admin, new AdminState(adminAddress, 100L).Serialize());
            world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
            world = EnsureToMakeGuild(world, guildAddress, GuildConfig.PlanetariumGuildOwner, validatorKey, height++);
            var guildMemberCount = 10;
            var migratedGuildMemberCount = 5;
            var guildMemberAddresses = Enumerable.Range(0, guildMemberCount).Select(
                _ => AddressUtil.CreateAgentAddress()).ToList();
            for (var i = 0; i < guildMemberCount; i++)
            {
                world = EnsureJoinLegacyPlanetariumGuild(world, guildMemberAddresses[i]);
            }

            for (var i = 0; i < migratedGuildMemberCount; i++)
            {
                var action = new MigrateStakeAndJoinGuild(guildMemberAddresses[i]);
                var actionContext = new ActionContext
                {
                    PreviousState = world,
                    Signer = adminAddress,
                    BlockIndex = height++,
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

        private static IWorld EnsureJoinLegacyPlanetariumGuild(IWorld world, AgentAddress guildParticipantAddress)
        {
            var planetariumGuildAddress = new GuildRepository(world, new ActionContext { })
                .GetJoinedGuild(GuildConfig.PlanetariumGuildOwner);
            var legacyParticipant = new LegacyGuildParticipant(planetariumGuildAddress.Value);

            return world
                .MutateAccount(Addresses.GuildParticipant, account => account.SetState(guildParticipantAddress, legacyParticipant.Bencoded))
                .MutateAccount(
                    Addresses.GuildMemberCounter,
                    account =>
                    {
                        BigInteger count = account.GetState(planetariumGuildAddress.Value) switch
                        {
                            Integer i => i.Value,
                            null => 0,
                            _ => throw new InvalidCastException(),
                        };

                        return account.SetState(planetariumGuildAddress.Value, (Integer)(count + 1));
                    });
        }
    }
}
