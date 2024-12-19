using System;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Action.ValidatorDelegation
{
    /// <summary>
    /// Set the commission percentage of the validator.
    /// </summary>
    [ActionType(TypeIdentifier)]
    public sealed class SetValidatorCommission : ActionBase
    {
        public const string TypeIdentifier = "set_validator_commission";

        public SetValidatorCommission() { }

        public SetValidatorCommission(BigInteger commissionPercentage)
        {
            CommissionPercentage = commissionPercentage;
        }

        public BigInteger CommissionPercentage { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
                .Add(CommissionPercentage));

        public override void LoadPlainValue(IValue plainValue)
        {
            var root = (Dictionary)plainValue;
            if (plainValue is not Dictionary ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not List values)
            {
                throw new InvalidCastException();
            }

            CommissionPercentage = (Integer)values[0];
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var validatorAddress = context.Signer;
            var world = context.PreviousState;
            var repository = new ValidatorRepository(world, context);
            repository.SetCommissionPercentage(validatorAddress, CommissionPercentage, context.BlockIndex);

            return repository.World;
        }
    }
}
