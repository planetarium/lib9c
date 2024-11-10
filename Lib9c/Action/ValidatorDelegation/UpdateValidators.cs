using System.Linq;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Types.Consensus;
using Nekoyume.ValidatorDelegation;

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
            }

            foreach (var activated in validators.Except(prevValidators))
            {
                var validatorDelegatee = repository.GetValidatorDelegatee(activated.OperatorAddress);
                validatorDelegatee.Activate();
                repository.SetValidatorDelegatee(validatorDelegatee);
            }

            return repository.World.SetValidatorSet(new ValidatorSet(validators));
        }
    }
}
