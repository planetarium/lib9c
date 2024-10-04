using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.ValidatorDelegation;
using Nekoyume.Delegation;
using System;
using System.Linq;
using System.Collections.Immutable;

namespace Nekoyume.Action.ValidatorDelegation
{
    public sealed class ReleaseValidatorUnbondings : ActionBase
    {
        public ReleaseValidatorUnbondings() { }

        public ReleaseValidatorUnbondings(Address validatorDelegatee)
        {
        }

        public override IValue PlainValue => Null.Value;

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(0L);

            var world = context.PreviousState;
            var repository = new ValidatorRepository(world, context);
            var unbondingSet = repository.GetUnbondingSet();
            var unbondings = unbondingSet.UnbondingsToRelease(context.BlockIndex);

            unbondings = unbondings.Select(unbonding => unbonding.Release(context.BlockIndex)).ToImmutableArray();

            foreach (var unbonding in unbondings)
            {
                switch (unbonding)
                {
                    case UnbondLockIn unbondLockIn:
                        repository.SetUnbondLockIn(unbondLockIn);
                        break;
                    case RebondGrace rebondGrace:
                        repository.SetRebondGrace(rebondGrace);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid unbonding type.");
                }
            }

            repository.SetUnbondingSet(unbondingSet.SetUnbondings(unbondings));

            return repository.World;
        }
    }
}
