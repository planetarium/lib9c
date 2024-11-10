using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.Model.Guild;
using Nekoyume.Model.State;
using Nekoyume.ValidatorDelegation;

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
            var guildRepository = new GuildRepository(world, context);

            var guildAddress = guildRepository.GetGuildParticipant(GuildConfig.PlanetariumGuildOwner).GuildAddress;
            var guild = guildRepository.GetGuild(guildAddress);
            var validatorDelegateeForGuildParticipant
                = guildRepository.GetValidatorDelegateeForGuildParticipant(guild.ValidatorAddress);

            // TODO: [GuildMigration] Replace below height when determined.
            validatorDelegateeForGuildParticipant.Metadata.UpdateUnbondingPeriod(LegacyStakeState.LockupInterval);
            guildRepository.SetValidatorDelegateeForGuildParticipant(validatorDelegateeForGuildParticipant);

            var repository = new ValidatorRepository(guildRepository);
            var validatorDelegatee = repository.GetValidatorDelegatee(guild.ValidatorAddress);

            // TODO: [GuildMigration] Replace below height when determined.
            validatorDelegatee.Metadata.UpdateUnbondingPeriod(LegacyStakeState.LockupInterval);
            repository.SetValidatorDelegatee(validatorDelegatee);

            return repository.World;
        }
    }
}
