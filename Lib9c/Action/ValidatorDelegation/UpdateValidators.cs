using System.Linq;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Types.Consensus;
using Nekoyume.ValidatorDelegation;
using Nekoyume.Model.Guild;

namespace Nekoyume.Action.ValidatorDelegation
{
    public sealed class UpdateValidators : ActionBase
    {
        public UpdateValidators() { }

        public override IValue PlainValue => Null.Value;

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IWorld Execute(IActionContext context)
        {
            var world = context.PreviousState;
            var prevValidators = world.GetValidatorSet().Validators;
            var repository = new ValidatorRepository(world, context);
            var validators = repository.GetValidatorList().ActiveSet();

            foreach (var deactivated in prevValidators.Except(validators))
            {
                var validatorDelegatee = repository.GetValidatorDelegatee(deactivated.OperatorAddress);
                validatorDelegatee.Deactivate();
                repository.SetValidatorDelegatee(validatorDelegatee);
                var guildRepository = new GuildRepository(repository.World, repository.ActionContext);
                var validatorDelegateeForGuildParticipant = guildRepository.GetValidatorDelegateeForGuildParticipant(deactivated.OperatorAddress);
                validatorDelegateeForGuildParticipant.Deactivate();
                guildRepository.SetValidatorDelegateeForGuildParticipant(validatorDelegateeForGuildParticipant);
                repository.UpdateWorld(guildRepository.World);
            }

            foreach (var activated in validators.Except(prevValidators))
            {
                var validatorDelegatee = repository.GetValidatorDelegatee(activated.OperatorAddress);
                validatorDelegatee.Activate();
                repository.SetValidatorDelegatee(validatorDelegatee);
                var guildRepository = new GuildRepository(repository.World, repository.ActionContext);
                var validatorDelegateeForGuildParticipant = guildRepository.GetValidatorDelegateeForGuildParticipant(activated.OperatorAddress);
                validatorDelegateeForGuildParticipant.Activate();
                guildRepository.SetValidatorDelegateeForGuildParticipant(validatorDelegateeForGuildParticipant);
                repository.UpdateWorld(guildRepository.World);
            }

            return repository.World.SetValidatorSet(new ValidatorSet(validators));
        }
    }
}
