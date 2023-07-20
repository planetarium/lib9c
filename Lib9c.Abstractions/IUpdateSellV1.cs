using System;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Abstractions
{
    public interface IUpdateSellV1
    {
        Guid OrderId { get; }
        Guid UpdateSellOrderId { get; }
        Guid TradableId { get; }
        Address SellerAvatarAddress { get; }
        string ItemSubType { get; }
        FungibleAssetValue Price { get; }
        int Count { get; }
    }
}
