using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Extensions;
using Lib9c.Model.State;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Module
{
    public static class GiftModule
    {
        public static List<int> GetClaimedGifts(this IWorldState state, Address avatarAddress)
        {
            var account = state.GetAccountState(Addresses.ClaimedGiftIds);
            return account.GetState(avatarAddress) is List rawIds
                ? rawIds.ToList(serialized => (int)(Integer)serialized)
                : new List<int>();
        }

        public static IWorld SetClaimedGifts(this IWorld world, Address avatarAddress, List<int> claimedGiftIds)
        {
            return world.MutateAccount(
                Addresses.ClaimedGiftIds,
                account => account.SetState(
                    avatarAddress,
                    claimedGiftIds.Aggregate(List.Empty, (current, giftId) => current.Add((Integer)giftId))
                )
            );
        }
    }
}
