using System;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    public class FungibleAssetValueOrder: OrderBase, IOrder
    {
        public FungibleAssetValueOrder(Address sellerAgentAddress, Address sellerAvatarAddress, Guid orderId, FungibleAssetValue price, Guid tradableId, long startedBlockIndex, FungibleAssetValue fav)  : base(orderId, tradableId, startedBlockIndex, startedBlockIndex + Order.ExpirationInterval)
        {
            SellerAgentAddress = sellerAgentAddress;
            SellerAvatarAddress = sellerAvatarAddress;
            Price = price;
            Asset = fav;
        }

        public FungibleAssetValueOrder(Dictionary serialized) : base(serialized)
        {
            Asset = serialized["a"].ToFungibleAssetValue();
        }

        public Order.OrderType Type => Order.OrderType.FungibleAssetValue;
        public Address SellerAgentAddress { get; }
        public Address SellerAvatarAddress { get; }
        public FungibleAssetValue Price { get; }
        public FungibleAssetValue Asset { get; }

        public void Validate(IAccountStateDelta state)
        {
            var balance = state.GetBalance(SellerAvatarAddress, Asset.Currency);
            if (balance < Asset)
            {
                throw new InsufficientBalanceException("", SellerAvatarAddress, balance);
            }
        }

        public IAccountStateDelta Sell(IAccountStateDelta state)
        {
            var recipient = Order.DeriveAddress(OrderId);
            return state.TransferAsset(SellerAvatarAddress, recipient, Asset);
        }

        public OrderDigest Digest(RuneSheet runeSheet)
        {
            var row = runeSheet.Values.First(r => r.Ticker == Asset.Currency.Ticker);
            return new OrderDigest(
                SellerAgentAddress,
                StartedBlockIndex,
                ExpiredBlockIndex,
                OrderId,
                TradableId,
                Price,
                0,
                0,
                row.Id,
                int.Parse(Asset.GetQuantityString(), CultureInfo.InvariantCulture)
            );

            throw new NotImplementedException();
        }

        public void ValidateCancelOrder(IAccountStateDelta state)
        {
            throw new NotImplementedException();
        }

        public int ValidateTransfer(AvatarState avatarState, Guid tradableId, FungibleAssetValue price,
            long blockIndex)
        {
            throw new NotImplementedException();
        }

        public FungibleAssetValue GetTax()
        {
            throw new NotImplementedException();
        }

        public OrderReceipt Transfer(AvatarState seller, AvatarState buyer, long blockIndex, IAccountStateDelta state)
        {
            throw new NotImplementedException();
        }

        public ITradableItem Cancel(IAccountStateDelta state, long blockIndex)
        {
            throw new NotImplementedException();
        }

        public override IValue Serialize()
        {
            var innerDictionary = ((Dictionary) base.Serialize())
                .SetItem(SellerAgentAddressKey, SellerAgentAddress.Serialize())
                .SetItem(SellerAvatarAddressKey, SellerAvatarAddress.Serialize())
                .SetItem(PriceKey, Price.Serialize())
                .SetItem(OrderTypeKey, Type.Serialize())
                .SetItem("a", Asset.Serialize());
            return new Dictionary(innerDictionary);
        }

    }
}
