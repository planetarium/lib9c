using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Lib9c.Model.Order
{
    public interface IOrder
    {
        public Guid OrderId { get; }
        public Guid TradableId { get; }
        public Order.OrderType Type { get; }
        public Address SellerAgentAddress { get; }
        public Address SellerAvatarAddress { get; }
        public FungibleAssetValue Price { get; }
        public long StartedBlockIndex { get; }
        public long ExpiredBlockIndex { get; }

        public IValue Serialize();
        public FungibleAssetValue GetTax();
    }
}
