using System.Collections.Generic;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Action
{
    public interface IClaimItems
    {
        public IReadOnlyList<(Address address, IReadOnlyList<FungibleAssetValue> fungibleAssetValues)> ClaimData { get; }
    }
}
