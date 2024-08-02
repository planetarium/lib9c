using Bencodex.Types;
using Libplanet.Action.State;
using Nekoyume.Extensions;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
using Nekoyume.PolicyAction.Tx.Begin;
using Nekoyume.TypedAddress;
using Serilog;

namespace Nekoyume.Action.Guild.Migration.Controls
{
    public static class GuildMigrationCtrl
    {
        /// <summary>
        /// Migrate the pledge to the guild if the <paramref name="target"/> has contracted pledge
        /// with Planetarium (<see cref="MeadConfig.PatronAddress"/>).
        /// </summary>
        /// <param name="world"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static (IWorld World, bool ShouldFail) MigratePlanetariumPledgeToGuild(IWorld world, AgentAddress target)
        {
            var logger = Log.ForContext(typeof(GuildMigrationCtrl));

            var planetariumPatronAddress = new AgentAddress(MeadConfig.PatronAddress);
            if (world.GetJoinedGuild(planetariumPatronAddress) is not { } planetariumGuildAddress)
            {
                logger.Warning("Planetarium seems not to make guild yet. Skip auto joining.");
                return (world, true);
            }

            if (!world.TryGetGuild(planetariumGuildAddress, out var planetariumGuild))
            {
                logger.Error(
                    "Planetarium address seems to join guild but it failed to fetch " +
                    "the guild. It seems a bug situation. It skips auto joining but " +
                    "you must investigate this issue.");
                return (world, true);
            }

            if (planetariumGuild.GuildMasterAddress != planetariumPatronAddress)
            {
                logger.Error(
                    "Planetarium address seems to join guild but it is the owner of the guild." +
                    "It seems a bug situation. It skips auto joining but " +
                    "you must investigate this issue.");
                return (world, true);
            }

            if (world.GetJoinedGuild(target) is { } joinedGuildAddress)
            {
                Log.ForContext<AutoJoinGuild>()
                    .Verbose(
                        "{Signer} is already joined to {Guild}. Skip auto joining.",
                        target,
                        joinedGuildAddress);
                return (world, true);
            }

            var pledgeAddress = target.GetPledgeAddress();

            // Patron contract structure:
            // [0] = PatronAddress
            // [1] = IsApproved
            // [2] = Mead amount to refill.
            if (!world.TryGetLegacyState(pledgeAddress, out List list) || list.Count < 3 || list[0].ToAddress() != MeadConfig.PatronAddress)
            {
                return (world, true);
            }

            return (world.JoinGuild(planetariumGuildAddress, target), false);
        }
    }
}
