using System;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Module.ValidatorDelegation;

namespace Nekoyume.Action.ValidatorDelegation
{
    public class RedelegateValidator : ActionBase
    {
        public const string TypeIdentifier = "redelegate_validator";

        private const string TargetKey = "t";

        public RedelegateValidator() { }

        public RedelegateValidator(
            Address srcValidatorDelegatee, Address dstValidatorDelegatee, BigInteger share)
        {
            SrcValidatorDelegatee = srcValidatorDelegatee;
            DstValidatorDelegatee = dstValidatorDelegatee;
            Share = share;
        }

        public Address SrcValidatorDelegatee { get; private set; }

        public Address DstValidatorDelegatee { get; private set; }

        public BigInteger Share { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
                .Add(SrcValidatorDelegatee.Bencoded)
                .Add(DstValidatorDelegatee.Bencoded)
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

            SrcValidatorDelegatee = new Address(values[0]);
            DstValidatorDelegatee = new Address(values[1]);
            Share = (Integer)values[2];
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;

            return world.RedelegateValidator(context, SrcValidatorDelegatee, DstValidatorDelegatee, Share);
        }
    }
}
