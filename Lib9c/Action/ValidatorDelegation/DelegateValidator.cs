using System;
using Bencodex.Types;
using Lib9c.Model.Guild;
using Lib9c.ValidatorDelegation;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Types.Assets;

namespace Lib9c.Action.ValidatorDelegation
{
    [ActionType(TypeIdentifier)]
    public sealed class DelegateValidator : ActionBase
    {
        public const string TypeIdentifier = "delegate_validator";

        public DelegateValidator() { }

        public DelegateValidator(FungibleAssetValue fav)
        {
            FAV = fav;
        }

        public FungibleAssetValue FAV { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
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

            FAV = new FungibleAssetValue(values[0]);
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            if (FAV.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(FAV), FAV, "Fungible asset value must be positive.");
            }

            var guildRepository = new GuildRepository(context.PreviousState, context);
            var guildDelegatee = guildRepository.GetDelegatee(context.Signer);
            var guildDelegator = guildRepository.GetDelegator(context.Signer);
            guildDelegator.Delegate(guildDelegatee, FAV, context.BlockIndex);

            var validatorRepository = new ValidatorRepository(guildRepository);
            var validatorDelegatee = validatorRepository.GetDelegatee(context.Signer);
            var validatorDelegator = validatorRepository.GetDelegator(context.Signer);
            validatorDelegatee.Bond(validatorDelegator, FAV, context.BlockIndex);

            return validatorRepository.World;
        }
    }
}
