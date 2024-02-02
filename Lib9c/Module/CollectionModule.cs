using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Module
{
    public static class CollectionModule
    {
        public static CollectionState GetCollectionState(this IWorldState worldState, Address address)
        {
            var serializedCollection = worldState.GetResolvedState(address, Addresses.Collection);
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

        public static IWorld SetCollectionState(this IWorld world, Address collection, CollectionState state)
        {
            var account = world.GetAccount(Addresses.Collection);
            account = account.SetState(collection, state.Bencoded);
            return world.SetAccount(Addresses.Collection, account);
        }
    }
}
