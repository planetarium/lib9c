using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.Model.Guild;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Lib9c;
using Nekoyume.Module.Guild;

namespace Nekoyume.Action.Guild.Migration
{
    // TODO: [GuildMigration] Remove this class when the migration is done.
    /// <summary>
    /// An action to migrate the planetarium guild.
    /// After migration, the planetarium guild now has a validator to delegate.
    /// </summary>
    [ActionType(TypeIdentifier)]
    public class MigratePlanetariumGuild : ActionBase
    {
        public const string TypeIdentifier = "migrate_planetarium_guild";

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
            var legacyGuildMaster = new LegacyGuildParticipant(guildMasterValue);
            var guildAddress = legacyGuildMaster.GuildAddress;

            // MigratePlanetariumGuild
            var guildValue = world
                .GetAccountState(Addresses.Guild)
                .GetState(guildAddress) as List;
            var legacyGuild = new LegacyGuild(guildValue);
            var guild = new Model.Guild.Guild(
                guildAddress,
                legacyGuild.GuildMasterAddress,
                context.Miner,
                repository);
            repository.SetGuild(guild);

            // MigratePlanetariumGuildMaster
            var guildParticipant = new GuildParticipant(
                GuildConfig.PlanetariumGuildOwner,
                guildAddress,
                repository);
            repository.SetGuildParticipant(guildParticipant);

            // Migrate delegation
            var guildGold = repository.GetBalance(guildParticipant.DelegationPoolAddress, Currencies.GuildGold);
            if (guildGold.RawValue > 0)
            {
                repository.Delegate(guildParticipant.Address, guildGold);
            }

            return repository.World;
        }
    }
}
