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
            var repository = new ValidatorRepository(world, context);
            var validators = repository.GetValidatorList();

            return world.SetValidatorSet(new ValidatorSet(validators.GetBonded()));
        }
    }
}
