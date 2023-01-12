using System.Collections.Generic;
using Libplanet;
using Libplanet.Action;

namespace Lib9c.Action
{
    /// <summary>
    /// Common interface used after <see cref="IBuy5"/>.
    /// </summary>
    /// <seealso cref="IBuy5"/>
    public interface IBuy5 : IAction
    {
        Address buyerAvatarAddress { get; }
        IEnumerable<IPurchaseInfo> purchaseInfos { get; }
    }
}
