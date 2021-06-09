using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class NonFungibleOrder : Order
    {
        public NonFungibleOrder(Address sellerAgentAddress, Address sellerAvatarAddress, Guid orderId, FungibleAssetValue price, Guid itemId, long startedBlockIndex) : base(sellerAgentAddress, sellerAvatarAddress, orderId, price, itemId, startedBlockIndex)
        {
        }

        public NonFungibleOrder(Dictionary serialized) : base(serialized)
        {
        }

        public override OrderType Type => OrderType.NonFungible;
    }
}
