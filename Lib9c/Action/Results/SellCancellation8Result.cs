using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Results
{
    [Serializable]
    public class SellCancellation8Result : AttachmentActionResult
    {
        public ShopItem shopItem;
        public Guid id;

        protected override string TypeId => "sellCancellation.result";

        public SellCancellation8Result()
        {
        }

        public SellCancellation8Result(Dictionary serialized) : base(serialized)
        {
            shopItem = new ShopItem((Dictionary) serialized["shopItem"]);
            id = serialized["id"].ToGuid();
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "shopItem"] = shopItem.Serialize(),
                [(Text) "id"] = id.Serialize()
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}