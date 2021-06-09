using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class FungibleOrder : Order
    {
        public readonly int ItemCount;

        public FungibleOrder(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid orderId,
            FungibleAssetValue price,
            Guid itemId,
            long startedBlockIndex,
            int itemCount
        ) : base(sellerAgentAddress,
            sellerAvatarAddress,
            orderId,
            price,
            itemId,
            startedBlockIndex
        )
        {
            ItemCount = itemCount;
        }

        public FungibleOrder(Dictionary serialized) : base(serialized)
        {
            ItemCount = serialized[TradableFungibleItemCountKey].ToInteger();
        }

        public override OrderType Type => OrderType.Fungible;

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) TradableFungibleItemCountKey] = ItemCount.Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002

        protected bool Equals(FungibleOrder other)
        {
            return base.Equals(other) && ItemCount == other.ItemCount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FungibleOrder) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ ItemCount;
            }
        }
    }
}
