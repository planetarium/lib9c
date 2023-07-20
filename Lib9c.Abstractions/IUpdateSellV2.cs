using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IUpdateSellV2
    {
        Address SellerAvatarAddress { get; }
        IEnumerable<IValue> UpdateSellInfos { get; }
    }
}
