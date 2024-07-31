using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;
using Serilog;

namespace Nekoyume.PolicyAction.Tx.Begin
{
    /// <summary>
    /// An action that automatically joins to Planetarium guild if it contracted pledge with Planetarium.
    /// </summary>
    public class AutoJoinGuild : ActionBase
    {
        public override IValue PlainValue => Null.Value;

        public override void LoadPlainValue(IValue plainValue)
        {
            throw new InvalidOperationException("Policy action shouldn't be serialized.");
        }

        public override IWorld Execute(IActionContext context)
        {
            var world = context.PreviousState;
            var logger = Log.ForContext<AutoJoinGuild>();
            if (!context.IsPolicyAction)
            {
                throw new InvalidOperationException(
                    "This action must be called when it is a policy action.");
            }

            var planetariumPatronAddress = new AgentAddress(MeadConfig.PatronAddress);
            if (world.GetJoinedGuild(planetariumPatronAddress) is not { } planetariumGuildAddress)
            {
                logger.Warning("Planetarium seems not to make guild yet. Skip auto joining.");
                return world;
            }

            if (!world.TryGetGuild(planetariumGuildAddress, out var planetariumGuild))
            {
                logger.Error(
                    "Planetarium address seems to join guild but it failed to fetch " +
                    "the guild. It seems a bug situation. It skips auto joining but " +
                    "you must investigate this issue.");
                return world;
            }

            if (planetariumGuild.GuildMasterAddress != planetariumPatronAddress)
            {
                logger.Error(
                    "Planetarium address seems to join guild but it is the owner of the guild." +
                    "It seems a bug situation. It skips auto joining but " +
                    "you must investigate this issue.");
                return world;
            }

            var signer = context.GetAgentAddress();
            if (world.GetJoinedGuild(signer) is { } joinedGuildAddress)
            {
                Log.ForContext<AutoJoinGuild>()
                    .Verbose(
                        "{Signer} is already joined to {Guild}. Skip auto joining.",
                        signer,
                        joinedGuildAddress);
                return world;
            }

            var pledgeAddress = signer.GetPledgeAddress();

            // Patron contract structure:
            // [0] = PatronAddress
            // [1] = IsApproved
            // [2] = Mead amount to refill.
            if (!world.TryGetLegacyState(pledgeAddress, out List list) || list.Count < 3 || list[0].ToAddress() != MeadConfig.PatronAddress)
            {
                return world;
            }

            return world.JoinGuild(planetariumGuildAddress, signer);
        }
    }
}
