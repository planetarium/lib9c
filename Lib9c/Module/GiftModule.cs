using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;

namespace Nekoyume.Module
{
    public static class GiftModule
    {
        public static List<int> GetClaimedGifts(this IWorldState state, Address avatarAddress)
        {
            var account = state.GetAccountState(Addresses.ClaimedGiftIds);
            return account.GetState(avatarAddress) is List rawIds
                ? rawIds.ToList(StateExtensions.ToInteger)
                : new List<int>();
        }

        public static IWorld SetClaimedGifts(this IWorld world, Address avatarAddress, List<int> claimedGiftIds)
        {
            var account = world.GetAccount(Addresses.ClaimedGiftIds);
            account = account.SetState(avatarAddress, claimedGiftIds.Aggregate(List.Empty, (current, giftId) => current.Add(giftId.Serialize())));
            return world.SetAccount(Addresses.ClaimedGiftIds, account);
        }
    }
}
