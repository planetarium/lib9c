using System;

#nullable disable
namespace Nekoyume.Model.Item
{
    public interface INonFungibleItem: ITradableItem
    {
        Guid NonFungibleId { get; }
    }
}
