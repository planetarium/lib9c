using System;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Action.ValidatorDelegation
{
    [ActionType(TypeIdentifier)]
    public sealed class UndelegateValidator : ActionBase
    {
        public const string TypeIdentifier = "undelegate_validator";

        public UndelegateValidator() { }

        public UndelegateValidator(Address validatorDelegatee, BigInteger share)
        {
            ValidatorDelegatee = validatorDelegatee;
            Share = share;
        }

        public Address ValidatorDelegatee { get; private set; }

        public BigInteger Share { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
                .Add(ValidatorDelegatee.Bencoded)
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

            ValidatorDelegatee = new Address(values[0]);
            Share = (Integer)values[1];
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            if (context.Signer != ValidatorDelegatee)
            {
                throw new InvalidAddressException(
                    $"{nameof(context.Signer)}({context.Signer}) is " +
                    $"not equal to {nameof(ValidatorDelegatee)}({ValidatorDelegatee})."
                );
            }

            var world = context.PreviousState;
            var repository = new ValidatorRepository(world, context);
            repository.UndelegateValidator(ValidatorDelegatee, Share);

            return repository.World;
        }
    }
}
