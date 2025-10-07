using Bencodex;

namespace Lib9c.Model.Collection
{
    public interface ICollectionMaterial: IBencodable
    {
        public MaterialType Type { get; }
        public int ItemId { get; set; }
        public int ItemCount { get; set; }
    }
}
