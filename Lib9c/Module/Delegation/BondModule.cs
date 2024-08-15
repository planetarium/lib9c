#nullable enable
using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Delegation;

namespace Nekoyume.Module.Delegation
{
    public static class BondModule
    {
        public static Bond GetBond(
            this IWorldState world, IDelegatee delegatee, Address delegatorAddress)
            => GetBond(world, delegatee.BondAddress(delegatorAddress));

        public static Bond GetBond(this IWorldState world, Address address)
            => TryGetBond(world, address, out var bond)
                ? bond!
                : throw new InvalidOperationException("Bond not found");

        public static bool TryGetBond(
            this IWorldState world, Address address, out Bond? bond)
        {
            try
            {
                var value = world.GetAccountState(Addresses.Bond).GetState(address);
                if (!(value is List list))
                {
                    bond = null;
                    return false;
                }

                bond = new Bond(address, list);
                return true;
            }
            catch
            {
                bond = null;
                return false;
            }
        }
    }
}
