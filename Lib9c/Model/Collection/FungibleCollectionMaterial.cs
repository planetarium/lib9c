using Bencodex.Types;

namespace Nekoyume.Model.Collection
{
    public class FungibleCollectionMaterial : ICollectionMaterial
    {
        public MaterialType Type => MaterialType.Fungible;
        public int ItemId { get; set; }
        public int ItemCount { get; set; }

        public IValue Serialize()
        {
            return List.Empty
                .Add((int)Type)
                .Add(ItemId)
                .Add(ItemCount);
        }

        public FungibleCollectionMaterial(List serialized)
        {
            ItemId = (Integer)serialized[1];
            ItemCount = (Integer)serialized[2];
        }

        public FungibleCollectionMaterial()
        {
        }
    }
}
