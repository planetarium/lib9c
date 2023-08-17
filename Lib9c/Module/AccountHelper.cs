using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Store.Trie;

namespace Nekoyume.Module
{
    public static class AccountHelper
    {
        /// <summary>
        /// Gets the account of address <paramref name="address"/> if it exists.
        /// If not, returns the legacy account.
        /// </summary>
        /// <param name="world">An <see cref="IWorld"/> instance of to get state from.</param>
        /// <param name="address">The <see cref="Address"/> of the state to get.</param>
        /// <returns>An <see cref="IAccount"/> instance of address <paramref name="address"/> if exits.
        /// If not, legacy account.</returns>
        internal static IAccount ResolveAccount(IWorld world, Address address)
        {
            var agents = world.GetAccount(address);
            if (agents.StateRootHash.Equals(MerkleTrie.EmptyRootHash))
            {
                agents = world.GetAccount(ReservedAddresses.LegacyAccount);
            }

            return agents;
        }
    }
}
