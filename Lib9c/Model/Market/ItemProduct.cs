using System;
using Bencodex.Types;
using Lib9c.Action;
using Lib9c.Model.Item;
using Lib9c.Model.State;

namespace Lib9c.Model.Market
{
    public class ItemProduct : Product
    {
        public ITradableItem TradableItem;
        public int ItemCount;

        public ItemProduct()
        {
        }

        public ItemProduct(List serialized) : base(serialized)
        {
            TradableItem = (ITradableItem) ItemFactory.Deserialize(serialized[6]);
            ItemCount = serialized[7].ToInteger();
        }

        public override IValue Serialize()
        {
            List serialized = (List) base.Serialize();
            return serialized
                .Add(TradableItem.Serialize())
                .Add(ItemCount.Serialize());
        }

        public override void Validate(IProductInfo productInfo)
        {
            base.Validate(productInfo);
            var itemProductInfo = (ItemProductInfo) productInfo;
            if (itemProductInfo.Legacy)
            {
                throw new NotSupportedException();
            }
            if (itemProductInfo.TradableId != TradableItem.TradableId)
            {
                throw new InvalidTradableIdException("");
            }

            if (itemProductInfo.ItemSubType != TradableItem.ItemSubType)
            {
                throw new InvalidItemTypeException("");
            }
        }
    }
}
