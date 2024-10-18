using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Action.ValidatorDelegation
{
    [ActionType(TypeIdentifier)]
    public sealed class DelegateValidator : ActionBase
    {
        public const string TypeIdentifier = "delegate_validator";

        public DelegateValidator() { }

        public DelegateValidator(Address validatorDelegatee, FungibleAssetValue fav)
        {
            ValidatorDelegatee = validatorDelegatee;
            FAV = fav;
        }

        public Address ValidatorDelegatee { get; private set; }

        public FungibleAssetValue FAV { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
                .Add(ValidatorDelegatee.Bencoded)
                .Add(FAV.Serialize()));

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
            FAV = new FungibleAssetValue(values[1]);
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
            repository.DelegateValidator(ValidatorDelegatee, FAV);

            return repository.World;
        }
    }
}
