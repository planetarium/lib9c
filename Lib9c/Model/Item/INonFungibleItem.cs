using System;

namespace Lib9c.Model.Item
{
    public interface INonFungibleItem: IItem
    {
        Guid NonFungibleId { get; }
        long RequiredBlockIndex { get; set; }
    }
}
