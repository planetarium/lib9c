using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Action;
using Nekoyume.Action.Guild.Migration;
using Nekoyume.Action.Guild.Migration.Controls;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
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
            if (!context.IsPolicyAction)
            {
                throw new InvalidOperationException(
                    "This action must be called when it is a policy action.");
            }

            var world = context.PreviousState;
            var repository = new GuildRepository(world, context);
            var signer = context.GetAgentAddress();

            try
            {
                GuildMigrationCtrl.MigratePlanetariumPledgeToGuild(repository, signer);
            }
            catch (GuildMigrationFailedException guildMigrationFailedException)
            {
                Log.ForContext<AutoJoinGuild>()
                    .Debug(
                        "Migration from pledge to guild failed but it just skips. {Message}",
                        guildMigrationFailedException.Message);
            }
            catch (Exception e)
            {
                Log.ForContext<AutoJoinGuild>()
                    .Error(
                        "Unexpected exception but it skips. You should debug this situation. {Message}",
                        e.Message);
            }

            return repository.World;
        }
    }
}
