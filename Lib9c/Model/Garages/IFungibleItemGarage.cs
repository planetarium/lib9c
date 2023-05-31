using Nekoyume.Model.Item;

namespace Nekoyume.Model.Garages
{
    public interface IFungibleItemGarage
    {
        IFungibleItem Item { get; }
        int Count { get; }
        void Add(int count);
    }
}
