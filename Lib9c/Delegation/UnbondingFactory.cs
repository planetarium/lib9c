using System;
using Bencodex.Types;

namespace Nekoyume.Delegation
{
    public static class UnbondingFactory
    {
        public static IUnbonding GetUnbondingFromRef(
            UnbondingRef reference, IDelegationRepository repository)
            => reference.UnbondingType switch
            {
                UnbondingType.UnbondLockIn => repository.GetUnlimitedUnbondLockIn(reference.Address),
                UnbondingType.RebondGrace => repository.GetUnlimitedRebondGrace(reference.Address),
                _ => throw new ArgumentException("Invalid unbonding type.")
            };

        public static IUnbonding GetUnbondingFromRef(
            IValue bencoded, IDelegationRepository repository)
            => GetUnbondingFromRef(new UnbondingRef(bencoded), repository);

        public static UnbondingRef ToReference(IUnbonding unbonding)
            => unbonding switch
            {
                UnbondLockIn unbondLockIn
                    => new UnbondingRef(unbonding.Address, UnbondingType.UnbondLockIn),
                RebondGrace rebondGrace
                    => new UnbondingRef(unbonding.Address, UnbondingType.RebondGrace),
                _ => throw new ArgumentException("Invalid unbonding type.")
            };
    }
}
