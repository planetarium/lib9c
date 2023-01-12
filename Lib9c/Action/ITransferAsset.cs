#nullable enable
using Libplanet;
using Libplanet.Assets;

namespace Lib9c.Action
{
    public interface ITransferAsset
    {
        Address Sender { get; }
        Address Recipient { get; }
        FungibleAssetValue Amount { get; }
        string? Memo { get; }
    }
}
