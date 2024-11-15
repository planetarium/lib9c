using System;
using Nekoyume.Delegation;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Module.ValidatorDelegation
{
    public static class ValidatorUnbondingModule
    {
        public static ValidatorRepository ReleaseUnbondings(
            this ValidatorRepository repository)
        {
            var context = repository.ActionContext;
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

            return repository;
        }
    }
}
