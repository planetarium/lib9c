using System;
using Bencodex.Types;
using Lib9c.Model.Guild;
using Lib9c.Module.Guild;
using Lib9c.ValidatorDelegation;
using Libplanet.Action;
using Libplanet.Action.State;

namespace Lib9c.Action.Guild.Migration
{
    // TODO: [GuildMigration] Remove this class when the migration is done.
    /// <summary>
    /// An action to migrate the planetarium validator.
    /// With this migration, planetarium validator is now active,
    /// and bonded FAVs are moved to active address.
    /// </summary>
    [ActionType(TypeIdentifier)]
    public class MigratePlanetariumValidator : ActionBase
    {
        public const string TypeIdentifier = "migrate_planetarium_validator";

        public MigratePlanetariumValidator()
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
            var guildAddress = guildRepository.GetJoinedGuild(GuildConfig.PlanetariumGuildOwner);
            if (guildAddress is not { } planetariumGuildAddress)
            {
                throw new InvalidOperationException("The planetarium guild not exists");
            }

            var planetariumGuild = guildRepository.GetGuild(
                planetariumGuildAddress);

            var validatorRepository = new ValidatorRepository(guildRepository);
            var validatorDelegatee = validatorRepository.GetDelegatee(
                planetariumGuild.ValidatorAddress);

            var validatorSet = world.GetValidatorSet();

            if (!validatorSet.ContainsPublicKey(validatorDelegatee.PublicKey))
            {
                throw new InvalidOperationException(
                    "The planetarium validator is not in the validator set.");
            }

            if (validatorDelegatee.IsActive)
            {
                throw new InvalidOperationException(
                    "The planetarium validator is already active.");
            }

            validatorDelegatee.Activate();
            validatorRepository.SetDelegatee(validatorDelegatee);

            guildRepository.UpdateWorld(validatorRepository.World);
            var guildDelegatee = guildRepository.GetDelegatee(
                planetariumGuild.ValidatorAddress);
            guildDelegatee.Activate();
            guildRepository.SetDelegatee(guildDelegatee);

            return guildRepository.World;
        }
    }
}
