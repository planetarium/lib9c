using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.Model.Guild;

namespace Nekoyume.Action.Guild.Migration
{
    // TODO: [GuildMigration] Remove this class when the migration is done.
    // Enable this action on PoS release.
    /// <summary>
    /// An action to migrate the planetarium guild.
    /// </summary>
    // [ActionType(TypeIdentifier)]
    public class MigratePlanetariumGuildUnbond : ActionBase
    {
        public const string TypeIdentifier = "migrate_planetarium_guild_unbond";

        [Obsolete("Don't call in code.", error: false)]
        public MigratePlanetariumGuildUnbond()
        {
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Null.Value);

        public override void LoadPlainValue(IValue plainValue)
        {
            if (plainValue is not Dictionary root ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Null)
            {
                throw new InvalidCastException();
            }
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var repository = new GuildRepository(world, context);

            var guildAddress = repository.GetGuildParticipant(GuildConfig.PlanetariumGuildOwner).GuildAddress;
            var guild = repository.GetGuild(guildAddress);

            // TODO: [GuildMigration] Replace below height when determined.
            guild.Metadata.UpdateUnbondingPeriod(0L);

            repository.SetGuild(guild);

            return repository.World;
        }
    }
}
