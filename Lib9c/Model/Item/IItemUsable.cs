using System;

namespace Nekoyume.Model.Item
{
    public interface IItemUsable : IItem
    {
        Guid ItemId { get; }
        Guid TradableId { get; }
        Guid NonFungibleId { get; }
    }
}
