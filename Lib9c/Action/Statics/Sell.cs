using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;
using Nekoyume.Model;
using Nekoyume.Model.Exceptions;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action.Statics
{
    public static class Sell
    {
        public static IAccountStateDelta Cancel(IAccountStateDelta states,
            UpdateSellInfo updateSellInfo, string addressesHex, AvatarState avatarState,
            OrderDigestListState digestList, IActionContext context, Address sellerAvatarAddress)
        {
            if (updateSellInfo.price.Sign < 0)
            {
                throw new InvalidPriceException($"{addressesHex} Aborted as the price is less than zero: {updateSellInfo.price}.");
            }

            var sw = new Stopwatch();
            var orderId = updateSellInfo.orderId;
            var tradableId = updateSellInfo.tradableId;
            var shopAddress = ShardedShopStateV2.DeriveAddress(updateSellInfo.itemSubType, orderId);

            // migration method
            avatarState.inventory.UnlockInvalidSlot(digestList, context.Signer, sellerAvatarAddress);
            avatarState.inventory.ReconfigureFungibleItem(digestList, tradableId);
            avatarState.inventory.LockByReferringToDigestList(digestList, tradableId, context.BlockIndex);

            // for sell cancel
            sw.Start();
            if (!states.TryGetState(shopAddress, out Dictionary shopStateDict))
            {
                throw new FailedLoadStateException($"{addressesHex}failed to load {nameof(ShardedShopStateV2)}({shopAddress}).");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex} UpdateSell Sell Cancel Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            if (!states.TryGetState(Order.DeriveAddress(orderId), out Dictionary orderDict))
            {
                throw new FailedLoadStateException($"{addressesHex} failed to load {nameof(Order)}({Order.DeriveAddress(updateSellInfo.orderId)}).");
            }

            var orderOnSale = OrderFactory.Deserialize(orderDict);
            orderOnSale.ValidateCancelOrder(avatarState, tradableId);
            orderOnSale.Cancel(avatarState, context.BlockIndex);
            if (context.BlockIndex < orderOnSale.ExpiredBlockIndex)
            {
                var shardedShopState = new ShardedShopStateV2(shopStateDict);
                shardedShopState.Remove(orderOnSale, context.BlockIndex);
                states = states.SetState(shopAddress, shardedShopState.Serialize());
            }

            digestList.Remove(orderOnSale.OrderId);
            sw.Stop();

            var expirationMail = avatarState.mailBox.OfType<OrderExpirationMail>()
                .FirstOrDefault(m => m.OrderId.Equals(orderId));
            if (!(expirationMail is null))
            {
                avatarState.mailBox.Remove(expirationMail);
            }

            return states.SetState(digestList.Address, digestList.Serialize());
        }
    }
}
