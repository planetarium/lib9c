using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Action.ValidatorDelegation
{
    [ActionType(TypeIdentifier)]
    public sealed class ClaimValidatorRewardSelf : ActionBase
    {
        public const string TypeIdentifier = "claim_validator_reward_self";

        public ClaimValidatorRewardSelf() { }

        public Address ValidatorDelegatee { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Null.Value);

        public override void LoadPlainValue(IValue plainValue)
        {
            var root = (Dictionary)plainValue;
            if (plainValue is not Dictionary ||
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
            var repository = new ValidatorRepository(world, context);
            var validatorDelegatee = repository.GetValidatorDelegatee(context.Signer);
            var validatorDelegator = repository.GetValidatorDelegator(context.Signer);
            validatorDelegator.ClaimReward(validatorDelegatee, context.BlockIndex);

            return repository.World;
        }
    }
}
