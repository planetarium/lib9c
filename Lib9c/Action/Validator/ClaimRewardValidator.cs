using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Module.Validator;

namespace Nekoyume.Action.Validator
{
    public class ClaimRewardValidator : ActionBase
    {
        public const string TypeIdentifier = "claim_reward_validator";

        private const string TargetKey = "t";

        public ClaimRewardValidator() { }

        public ClaimRewardValidator(Address validatorDelegatee)
        {
            ValidatorDelegatee = validatorDelegatee;
        }

        public Address ValidatorDelegatee { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
                .Add(ValidatorDelegatee.Bencoded));

        public override void LoadPlainValue(IValue plainValue)
        {
            var root = (Dictionary)plainValue;
            if (plainValue is not Dictionary ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not List values)
            {
                throw new InvalidCastException();
            }

            ValidatorDelegatee = new Address(values[0]);
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);

            var world = context.PreviousState;

            return world.ClaimRewardValidator(context, ValidatorDelegatee);
        }
    }
}
