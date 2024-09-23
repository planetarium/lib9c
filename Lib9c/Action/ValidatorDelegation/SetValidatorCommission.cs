using System;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Action.ValidatorDelegation
{
    [ActionType(TypeIdentifier)]
    public sealed class SetValidatorCommission : ActionBase
    {
        public const string TypeIdentifier = "set_validator_commission";

        public SetValidatorCommission() { }

        public SetValidatorCommission(Address validatorDelegatee, BigInteger commissionPercentage)
        {
            ValidatorDelegatee = validatorDelegatee;
            CommissionPercentage = commissionPercentage;
        }

        public Address ValidatorDelegatee { get; private set; }

        public BigInteger CommissionPercentage { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
                .Add(ValidatorDelegatee.Bencoded)
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

            ValidatorDelegatee = new Address(values[0]);
            CommissionPercentage = (Integer)values[1];
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var repository = new ValidatorRepository(world, context);
            repository.SetCommissionPercentage(ValidatorDelegatee, CommissionPercentage);

            return repository.World;
        }
    }
}
