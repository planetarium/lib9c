#nullable enable

using System.Collections.Generic;
using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IRapidCombinationV2
    {
        Address AvatarAddress { get; }
        List<int> SlotIndexList { get; }
    }
}
