using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Results
{
    [Serializable]
    public class BuyerResult : AttachmentActionResult
    {
        public ShopItem shopItem;
        public Guid id;

        protected override string TypeId => "buy.buyerResult";

        public BuyerResult()
        {
        }

        public BuyerResult(Bencodex.Types.Dictionary serialized) : base(serialized)
        {
            shopItem = new ShopItem((Bencodex.Types.Dictionary) serialized["shopItem"]);
            id = serialized["id"].ToGuid();
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "shopItem"] = shopItem.Serialize(),
                [(Text) "id"] = id.Serialize(),
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
