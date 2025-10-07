using Lib9c.Model.Item;

namespace Lib9c.Model.Garages
{
    public interface IFungibleItemGarage : IGarage<IFungibleItemGarage, int>
    {
        IFungibleItem Item { get; }
        int Count { get; }
    }
}
