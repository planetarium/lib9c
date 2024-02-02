using Bencodex.Types;

namespace Nekoyume.Model.Collection
{
    public interface ICollectionMaterial
    {
        public MaterialType Type { get; }
        public int ItemId { get; set; }
        public int ItemCount { get; set; }

        public IValue Serialize();
    }
}
