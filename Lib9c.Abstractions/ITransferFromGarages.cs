#nullable enable

using System.Linq;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Assets;

namespace Lib9c.Abstractions
{
    public interface ITransferFromGarages
    {
        IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>? FungibleAssetValues
        {
            get;
        }

        Address? RecipientInventoryAddr { get; }
        IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>? FungibleIdAndCounts { get; }
    }
}
