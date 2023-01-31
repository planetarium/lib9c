using Bencodex.Types;

namespace Nekoyume.Model.Market
{
    public static class ProductFactory
    {
        public static Product Deserialize(List serialized)
        {
            if (serialized.Count == 4)
            {
                return new FavProduct(serialized);
            }

            return new ItemProduct(serialized);
        }
    }
}
