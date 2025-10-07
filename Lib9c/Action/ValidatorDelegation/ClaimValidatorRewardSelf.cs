using System;
using Bencodex.Types;
using Lib9c.Model.Guild;
using Lib9c.ValidatorDelegation;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Action.ValidatorDelegation
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
            var validatorDelegatee = repository.GetDelegatee(context.Signer);
            var validatorDelegator = repository.GetDelegator(context.Signer);
            validatorDelegator.ClaimReward(validatorDelegatee, context.BlockIndex);

            var guildRepository = new GuildRepository(repository);
            var guildDelegatee = guildRepository.GetDelegatee(context.Signer);
            var guildDelegator = guildRepository.GetDelegator(context.Signer);
            guildDelegator.ClaimReward(guildDelegatee, context.BlockIndex);

            return guildRepository.World;
        }
    }
}
