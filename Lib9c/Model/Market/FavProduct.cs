using Bencodex.Types;
using Lib9c.Model.State;
using Libplanet.Types.Assets;

namespace Lib9c.Model.Market
{
    public class FavProduct : Product
    {
        public FungibleAssetValue Asset;

        public FavProduct()
        {
        }

        public FavProduct(List serialized) : base(serialized)
        {
            Asset = serialized[6].ToFungibleAssetValue();
        }

        public override IValue Serialize()
        {
            List serialized = (List) base.Serialize();
            return serialized.Add(Asset.Serialize());
        }

    }
}
