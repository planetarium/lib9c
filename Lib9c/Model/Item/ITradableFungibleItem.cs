using System;

#nullable disable
namespace Nekoyume.Model.Item
{
    public interface ITradableFungibleItem : ITradableItem, IFungibleItem, ICloneable
    {
    }
}
