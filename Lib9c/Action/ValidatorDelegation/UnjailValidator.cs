using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.ValidatorDelegation;
using Nekoyume.Model.Guild;

namespace Nekoyume.Action.ValidatorDelegation
{
    [ActionType(TypeIdentifier)]
    public sealed class UnjailValidator : ActionBase
    {
        public const string TypeIdentifier = "unjail_validator";

        public UnjailValidator() { }

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
            var validatorRepository = new ValidatorRepository(world, context);
            var validatorDelegatee = validatorRepository.GetDelegatee(context.Signer);
            validatorDelegatee.Unjail(context.BlockIndex);

            var guildRepository = new GuildRepository(
                validatorRepository.World, validatorRepository.ActionContext);
            var guildDelegatee = guildRepository.GetDelegatee(context.Signer);
            guildDelegatee.Unjail(context.BlockIndex);

            return guildRepository.World;
        }
    }
}
