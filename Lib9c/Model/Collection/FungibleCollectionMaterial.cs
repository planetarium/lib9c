using Bencodex.Types;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.Collection
{
    public class FungibleCollectionMaterial : ICollectionMaterial
    {
        public MaterialType Type => MaterialType.Fungible;

        public int ItemId { get; set; }

        public int ItemCount { get; set; }

        public IValue Bencoded => List.Empty
            .Add((int) Type)
            .Add(ItemId)
            .Add(ItemCount);

        public FungibleCollectionMaterial(List serialized)
        {
            ItemId = (Integer)serialized[1];
            ItemCount = (Integer)serialized[2];
        }

        public FungibleCollectionMaterial(IValue bencoded) : this((List) bencoded)
        {
        }

        public FungibleCollectionMaterial()
        {
        }
    }
}
