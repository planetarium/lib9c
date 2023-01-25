using Bencodex.Types;
using Libplanet.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Market
{
    public class FavProduct : Product
    {
        public FungibleAssetValue Asset;

        public FavProduct()
        {
        }

        public FavProduct(List serialized) : base(serialized)
        {
            Asset = serialized[2].ToFungibleAssetValue();
        }

        public override IValue Serialize()
        {
            List serialized = (List) base.Serialize();
            return serialized.Add(Asset.Serialize());
        }

    }
}
