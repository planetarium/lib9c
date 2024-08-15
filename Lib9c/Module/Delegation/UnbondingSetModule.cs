#nullable enable
using System;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Delegation;

namespace Nekoyume.Module.Delegation
{
    public static class UnbondingSetModule
    {
        public static IWorld ReleaseUnbondings(this IWorld world, IActionContext context)
        {
            var repository = new DelegationRepository(world, context);
            var unbondingSet = repository.GetUnbondingSet();
            var unbondings = unbondingSet.UnbondingsToRelease(context.BlockIndex);

            IUnbonding released;
            foreach (var unbonding in unbondings)
            {
                released = unbonding.Release(context.BlockIndex);

                switch (released)
                {
                    case UnbondLockIn unbondLockIn:
                        repository.SetUnbondLockIn(unbondLockIn);
                        break;
                    case RebondGrace rebondGrace:
                        repository.SetRebondGrace(rebondGrace);
                        break;
                    default:
                        throw new ArgumentException("Invalid unbonding type.");
                }

                unbondingSet = unbondingSet.SetUnbonding(released);
            }

            repository.SetUnbondingSet(unbondingSet);

            return repository.World;
        }
    }
}
