using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Action;
using Lib9c.Model.State;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Serilog;

namespace Lib9c.Module
{
    /// <summary>
    /// Provides utility methods for working with collection states in the world state.
    /// </summary>
    public static class CollectionModule
    {
        /// <summary>
        /// Get the CollectionState for a given address from the world state.
        /// </summary>
        /// <param name="worldState">The world state object.</param>
        /// <param name="address">The address to retrieve the CollectionState.</param>
        /// <returns>The CollectionState corresponding to the address.</returns>
        /// <exception cref="FailedLoadStateException">Thrown when the Collection state for the given address is not found in the world state.</exception>
        /// <exception cref="InvalidCastException">Thrown when the serialized Collection state is not in the correct format.</exception>
        public static CollectionState GetCollectionState(this IWorldState worldState, Address address)
        {
            var serializedCollection = worldState.GetAccountState(Addresses.Collection).GetState(address);
            if (serializedCollection is null)
            {
                var msg = $"No Collection state ({address.ToHex()})";
                throw new FailedLoadStateException(msg);
            }

            try
            {
                if (serializedCollection is List list)
                {
                    return new CollectionState(list);
                }

                throw new InvalidCastException(
                    "Serialized Collection state must be a list.");
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid Collection state ({0}): {1}",
                    address.ToHex(),
                    serializedCollection
                );
                throw;
            }
        }

        /// <summary>
        /// Sets the state of a collection in the world.
        /// </summary>
        /// <param name="collection">The address of the collection.</param>
        /// <param name="state">The state of the collection.</param>
        /// <returns>The updated world with the updated collection state.</returns>
        public static IWorld SetCollectionState(this IWorld world, Address collection, CollectionState state)
        {
            var account = world.GetAccount(Addresses.Collection);
            account = account.SetState(collection, state.Bencoded);
            return world.SetAccount(Addresses.Collection, account);
        }

        /// <summary>
        /// Tries to get the collection state for a specific address from the given world state.
        /// </summary>
        /// <param name="worldState">The world state from which to get the collection state.</param>
        /// <param name="address">The address for which to retrieve the collection state.</param>
        /// <param name="collectionState">The resulting collection state, if found.</param>
        /// <returns>True if the collection state is found, otherwise false.</returns>
        public static bool TryGetCollectionState(this IWorldState worldState, Address address, out CollectionState collectionState)
        {
            try
            {
                collectionState = GetCollectionState(worldState, address);
                return true;
            }
            catch (Exception)
            {
                collectionState = null;
                return false;
            }
        }

        /// <summary>
        /// Retrieves the collection states for the given addresses from the world state.
        /// </summary>
        /// <param name="worldState">The world state used to retrieve the collection states.</param>
        /// <param name="addresses">The list of addresses to retrieve the collection states for.</param>
        /// <returns>A dictionary of Address and CollectionState pairs representing the collection states
        /// for the given addresses,
        /// or an empty dictionary for addresses that do not have a collection state.</returns>
        public static Dictionary<Address, CollectionState> GetCollectionStates(
            this IWorldState worldState,
            IReadOnlyList<Address> addresses)
        {
            var result = new Dictionary<Address, CollectionState>();
            IReadOnlyList<IValue> values =
                worldState
                    .GetAccountState(Addresses.Collection)
                    .GetStates(addresses);
            for (int i = 0; i < addresses.Count; i++)
            {
                var serialized = values[i];
                var address = addresses[i];
                if (serialized is List bencoded)
                {
                    result.TryAdd(address, new CollectionState(bencoded));
                }
            }

            return result;
        }
    }
}
