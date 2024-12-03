using System;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.ValidatorDelegation;
using Nekoyume.Model.Guild;

namespace Nekoyume.Action.ValidatorDelegation
{
    [ActionType(TypeIdentifier)]
    public sealed class UndelegateValidator : ActionBase
    {
        public const string TypeIdentifier = "undelegate_validator";

        public UndelegateValidator() { }

        public UndelegateValidator(BigInteger share)
        {
            Share = share;
        }

        public BigInteger Share { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
                .Add(Share));

        public override void LoadPlainValue(IValue plainValue)
        {
            var root = (Dictionary)plainValue;
            if (plainValue is not Dictionary ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not List values)
            {
                throw new InvalidCastException();
            }

            Share = (Integer)values[0];
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            if (Share.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Share), Share, "Share must be positive.");
            }

            var guildRepository = new GuildRepository(context.PreviousState, context);
            var guildDelegatee = guildRepository.GetGuildDelegatee(context.Signer);
            var guildDelegator = guildRepository.GetGuildDelegator(context.Signer);
            guildDelegator.Undelegate(guildDelegatee, Share, context.BlockIndex);

            var validatorRepository = new ValidatorRepository(guildRepository);
            var validatorDelegatee = validatorRepository.GetValidatorDelegatee(context.Signer);
            var validatorDelegator = validatorRepository.GetValidatorDelegator(context.Signer);
            validatorDelegatee.Unbond(validatorDelegator, Share, context.BlockIndex);

            return validatorRepository.World;
        }
    }
}
