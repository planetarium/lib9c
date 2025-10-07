#nullable enable
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Module
{
    public static class StateResolver
    {
        /// <summary>
        /// Gets the account of address <paramref name="address"/> if it exists.
        /// If not, returns from the legacy account.
        /// </summary>
        /// <param name="world">An <see cref="IWorld"/> instance of to get state from.</param>
        /// <param name="address">The <see cref="Address"/> of the state to get.</param>
        /// <param name="accountAddress">The <see cref="Address"/> of the account to get.</param>
        /// <returns>An <see cref="IAccountState"/> instance of address <paramref name="address"/> if exits.
        /// If not, legacy account.</returns>
        public static IValue? GetResolvedState(this IWorldState world, Address address, Address accountAddress)
        {
            IAccountState account = world.GetAccountState(accountAddress);
            IValue? state = account.GetState(address);
            return state ?? world.GetAccountState(ReservedAddresses.LegacyAccount).GetState(address);
        }

        /// <summary>
        /// Gets the account of address <paramref name="address"/> if it exists.
        /// If not, returns from the legacy account with <paramref name="legacyAddress"/>.
        /// </summary>
        /// <param name="world">An <see cref="IWorld"/> instance of to get state from.</param>
        /// <param name="address">The <see cref="Address"/> of the state to get.</param>
        /// <param name="accountAddress">The <see cref="Address"/> of the account to get.</param>
        /// <param name="legacyAddress">The <see cref="Address"/> of the state to get
        /// from the legacy account.</param>
        /// <returns>An <see cref="IAccountState"/> instance of address <paramref name="address"/> if exits.
        /// If not, legacy account.</returns>
        public static IValue? GetResolvedState(this IWorldState world, Address address, Address accountAddress, Address legacyAddress)
        {
            IAccountState account = world.GetAccountState(accountAddress);
            IValue? state = account.GetState(address);
            return state ?? world.GetAccountState(ReservedAddresses.LegacyAccount).GetState(legacyAddress);
        }
    }
}
