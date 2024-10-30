using System;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.Model.Guild;
using Nekoyume.Action.Guild.Migration.LegacyModels;

namespace Nekoyume.Action.Guild.Migration
{
    // TODO: [GuildMigration] Remove this class when the migration is done.
    /// <summary>
    /// An action to migrate the planetarium guild.
    /// </summary>
    [ActionType(TypeIdentifier)]
    public class MigratePlanetariumGuild : ActionBase
    {
        public const string TypeIdentifier = "migrate_planetarium_guild";

        [Obsolete("Don't call in code.", error: false)]
        public MigratePlanetariumGuild()
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

            // Get Guild address
            var guildMasterValue = world
                .GetAccountState(Addresses.GuildParticipant)
                .GetState(GuildConfig.PlanetariumGuildOwner) as List;
            var guildAddress = new LegacyGuildParticipant(guildMasterValue).GuildAddress;

            // MigratePlanetariumGuild
            var guildValue = world
                .GetAccountState(Addresses.Guild)
                .GetState(guildAddress) as List;
            var legacyGuild = new LegacyGuild(guildValue);
            var guild = new Model.Guild.Guild(
                guildAddress,
                legacyGuild.GuildMasterAddress,
                ValidatorConfig.PlanetariumValidator,
                Currencies.GuildGold,
                repository);
            repository.SetGuild(guild);

            return repository.World;
        }
    }
}
