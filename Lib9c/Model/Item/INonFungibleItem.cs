using System;

namespace Lib9c.Model.Item
{
    public interface INonFungibleItem: ITradableItem
    {
        Guid NonFungibleId { get; }
    }
}
