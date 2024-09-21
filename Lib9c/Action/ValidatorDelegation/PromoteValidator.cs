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
    public class PromoteValidator : ActionBase
    {
        public const string TypeIdentifier = "promote_validator";

        public PromoteValidator() { }

        public PromoteValidator(PublicKey publicKey, FungibleAssetValue fav)
        {
            PublicKey = publicKey;
            FAV = fav;
        }

        public PublicKey PublicKey { get; private set; }

        public FungibleAssetValue FAV { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
                .Add(PublicKey.Format(true))
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

            PublicKey = new PublicKey(((Binary)values[0]).ByteArray);
            FAV = new FungibleAssetValue(values[1]);
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var repository = new ValidatorRepository(world, context);

            repository.CreateValidatorDelegatee(context, PublicKey);
            repository.DelegateValidator(context, context.Signer, FAV);

            return repository.World;
        }
    }
}
