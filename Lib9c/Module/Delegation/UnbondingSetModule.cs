#nullable enable
using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
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

        public static IWorld Release(this IWorld world, IActionContext context)
        {
            var unbondingSet = world.GetUnbondingSet();
            var releaseds = world.ReleaseUnbondings(context, unbondingSet);
            foreach (var released in releaseds)
            {
                world = released switch
                {
                    UnbondLockIn unbondLockIn => world.SetUnbondLockIn(unbondLockIn),
                    RebondGrace rebondGrace => world.SetRebondGrace(rebondGrace),
                    _ => throw new ArgumentException("Invalid unbonding type.")
                };

                unbondingSet = unbondingSet.SetUnbonding(released);
            }

            return world.SetUnbondingSet(unbondingSet);
        }

        private static IUnbonding[] ReleaseUnbondings(
            this IWorld world, IActionContext context, UnbondingSet unbondingSet)
            => unbondingSet.ReleaseUnbondings(
                context.BlockIndex,
                (address, type) => world.GetAccount(AccountAddress(type)).GetState(address)
                    ?? throw new FailedLoadStateException(
                        $"Tried to release unbonding on {address}, but unbonding does not exist."));

        private static IWorld SetUnbondingSet(this IWorld world, UnbondingSet unbondingSet)
            => world.MutateAccount(
                Addresses.UnbondingSet,
                account => account.SetState(UnbondingSet.Address, unbondingSet.Bencoded));

        private static IWorld SetUnbondLockIn(
            this IWorld world, UnbondLockIn unbondLockIn)
            => world.MutateAccount(
                Addresses.UnbondLockIn,
                account => unbondLockIn.IsEmpty
                    ? account.RemoveState(unbondLockIn.Address)
                    : account.SetState(unbondLockIn.Address, unbondLockIn.Bencoded));

        public static IWorld SetRebondGrace(
            this IWorld world, RebondGrace rebondGrace)
            => world.MutateAccount(
                Addresses.RebondGrace,
                account => rebondGrace.IsEmpty
                    ? account.RemoveState(rebondGrace.Address)
                    : account.SetState(rebondGrace.Address, rebondGrace.Bencoded));

        private static Address AccountAddress(Type type) => type switch
        {
            var t when t == typeof(UnbondLockIn) => Addresses.UnbondLockIn,
            var t when t == typeof(RebondGrace) => Addresses.RebondGrace,
            _ => throw new ArgumentException("Invalid unbonding type.")
        };
    }
}
