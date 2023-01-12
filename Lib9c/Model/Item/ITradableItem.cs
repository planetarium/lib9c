using System;

namespace Lib9c.Model.Item
{
    public interface ITradableItem: IItem
    {
        Guid TradableId { get; }

        long RequiredBlockIndex { get; set; }
    }
}
