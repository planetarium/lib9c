using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Types.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Results
{
    [Serializable]
    public class SellerResult : AttachmentActionResult
    {
        public ShopItem shopItem;
        public Guid id;
        public FungibleAssetValue gold;

        protected override string TypeId => "buy.sellerResult";

        public SellerResult()
        {
        }

        public SellerResult(Bencodex.Types.Dictionary serialized) : base(serialized)
        {
            shopItem = new ShopItem((Bencodex.Types.Dictionary) serialized["shopItem"]);
            id = serialized["id"].ToGuid();
            gold = serialized["gold"].ToFungibleAssetValue();
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "shopItem"] = shopItem.Serialize(),
                [(Text) "id"] = id.Serialize(),
                [(Text) "gold"] = gold.Serialize(),
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
