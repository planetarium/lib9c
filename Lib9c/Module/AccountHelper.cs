#nullable enable
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Store.Trie;

namespace Nekoyume.Module
{
    public static class AccountHelper
    {
        /// <summary>
        /// Gets the account of address <paramref name="address"/> if it exists.
        /// If not, returns from the legacy account.
        /// </summary>
        /// <param name="world">An <see cref="IWorld"/> instance of to get state from.</param>
        /// <param name="address">The <see cref="Address"/> of the state to get.</param>
        /// <param name="accountAddress">The <see cref="Address"/> of the account to get.</param>
        /// <returns>An <see cref="IAccount"/> instance of address <paramref name="address"/> if exits.
        /// If not, legacy account.</returns>
        public static IValue? Resolve(IWorldState world, Address address, Address accountAddress)
        {
            IAccount account = world.GetAccount(accountAddress);
            IValue? state = account.GetState(address);
            return state ?? world.GetAccount(ReservedAddresses.LegacyAccount).GetState(address);
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
        /// <returns>An <see cref="IAccount"/> instance of address <paramref name="address"/> if exits.
        /// If not, legacy account.</returns>
        public static IValue? Resolve(IWorldState world, Address address, Address accountAddress, Address legacyAddress)
        {
            IAccount account = world.GetAccount(accountAddress);
            IValue? state = account.GetState(address);
            return state ?? world.GetAccount(ReservedAddresses.LegacyAccount).GetState(legacyAddress);
        }
    }
}
