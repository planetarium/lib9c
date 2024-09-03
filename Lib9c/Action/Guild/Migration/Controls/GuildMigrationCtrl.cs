using Bencodex.Types;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Action.Guild.Migration.Controls
{
    public static class GuildMigrationCtrl
    {
        /// <summary>
        /// Migrate the pledge to the guild if the <paramref name="target"/> has contracted pledge
        /// with Planetarium (<see cref="GuildConfig.PlanetariumGuildOwner"/>).
        /// </summary>
        /// <param name="world"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        /// <exception cref="GuildMigrationFailedException">Migration to guild from pledge failed.</exception>
        public static void MigratePlanetariumPledgeToGuild(GuildRepository repository, AgentAddress target)
        {
            if (repository.GetJoinedGuild(GuildConfig.PlanetariumGuildOwner) is not
                { } planetariumGuildAddress)
            {
                throw new GuildMigrationFailedException("Planetarium guild is not found.");
            }

            if (!repository.TryGetGuild(planetariumGuildAddress, out var planetariumGuild))
            {
                throw new GuildMigrationFailedException("Planetarium guild is not found.");
            }

            if (planetariumGuild.GuildMasterAddress != GuildConfig.PlanetariumGuildOwner)
            {
                throw new GuildMigrationFailedException("Unexpected guild master.");
            }

            if (repository.GetJoinedGuild(target) is not null)
            {
                throw new GuildMigrationFailedException("Already joined to other guild.");
            }

            var pledgeAddress = target.GetPledgeAddress();

            // Patron contract structure:
            // [0] = PatronAddress
            // [1] = IsApproved
            // [2] = Mead amount to refill.
            if (!repository.World.TryGetLegacyState(pledgeAddress, out List list) || list.Count < 3 ||
                list[0] is not Binary || list[0].ToAddress() != MeadConfig.PatronAddress ||
                list[1] is not Boolean approved || !approved)
            {
                throw new GuildMigrationFailedException("Unexpected pledge structure.");
            }

            repository.JoinGuild(planetariumGuildAddress, target);
        }
    }
}
