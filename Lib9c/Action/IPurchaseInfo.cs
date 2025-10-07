using System;
using Lib9c.Model.Item;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Action
{
    public interface IPurchaseInfo
    {
        Guid? OrderId { get; }
        Address SellerAgentAddress { get; }
        Address SellerAvatarAddress { get; }
        FungibleAssetValue Price { get; }
        ItemSubType ItemSubType { get; }
    }
}
