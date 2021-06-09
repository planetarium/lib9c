using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    public static class OrderFactory
    {
        public static Order Create(ShopItem shopItem, long blockIndex)
        {
            Address sellerAgentAddress = shopItem.SellerAgentAddress;
            Address sellerAvatarAddress = shopItem.SellerAvatarAddress;
            Guid orderId = shopItem.ProductId;
            FungibleAssetValue price = shopItem.Price;
            var itemId = (shopItem.ItemUsable?.ItemId ?? shopItem.Costume?.ItemId) ??
                         shopItem.TradableFungibleItem.TradableId;
            if (shopItem.TradableFungibleItem is null)
            {
                return CreateNonFungibleOrder(sellerAgentAddress, sellerAvatarAddress, orderId, price, itemId,
                    blockIndex);
            }

            return CreateFungibleOrder(sellerAgentAddress, sellerAvatarAddress, orderId, price, itemId, blockIndex,
                shopItem.TradableFungibleItemCount);
        }

        public static NonFungibleOrder CreateNonFungibleOrder(
            Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid orderId,
            FungibleAssetValue price,
            Guid itemId,
            long blockIndex
        )
        {
            return new NonFungibleOrder(sellerAgentAddress, sellerAvatarAddress, orderId, price, itemId, blockIndex);
        }

        public static FungibleOrder CreateFungibleOrder(
            Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid orderId,
            FungibleAssetValue price,
            Guid itemId,
            long blockIndex,
            int count
        )
        {
            return new FungibleOrder(sellerAgentAddress, sellerAvatarAddress, orderId, price, itemId, blockIndex, count);
        }

        public static Order Deserialize(Dictionary dictionary)
        {
            return dictionary[OrderTypeKey].ToEnum<Order.OrderType>().Equals(Order.OrderType.Fungible)
                ? (Order) new FungibleOrder(dictionary)
                : new NonFungibleOrder(dictionary);
        }
    }
}
