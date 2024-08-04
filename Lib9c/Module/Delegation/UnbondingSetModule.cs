#nullable enable
using Bencodex.Types;
using Libplanet.Action.State;
using Nekoyume.Delegation;
using Nekoyume.Extensions;

namespace Nekoyume.Module.Delegation
{
    public static class UnbondingSetModule
    {
        public static UnbondingSet GetUnbondingSet(this IWorldState world)
            => TryGetUnbondingSet(world, out var unbondingSet)
                ? unbondingSet!
                : new UnbondingSet();

        public static bool TryGetUnbondingSet(
            this IWorldState world, out UnbondingSet? unbondingSet)
        {
            try
            {
                var value = world.GetAccountState(Addresses.UnbondingSet)
                    .GetState(UnbondingSet.Address);
                if (!(value is List list))
                {
                    unbondingSet = null;
                    return false;
                }

                unbondingSet = new UnbondingSet(list);
                return true;
            }
            catch
            {
                unbondingSet = null;
                return false;
            }
        }

        public static IWorld SetUnbondingSet(this IWorld world, UnbondingSet unbondingSet)
            => world.MutateAccount(
                Addresses.UnbondingSet,
                account => account.SetState(UnbondingSet.Address, unbondingSet.Bencoded));
    }
}
