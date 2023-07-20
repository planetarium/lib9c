using System;
using Libplanet.Action;
using Libplanet.Crypto;

namespace Nekoyume.Action
{
    /// <summary>
    /// Common interface used before <see cref="IBuy5"/>.
    /// </summary>
    /// <seealso cref="IBuy5"/>
    public interface IBuy0 : IAction
    {
        Address buyerAvatarAddress { get; }
        Address sellerAgentAddress { get; }
        Address sellerAvatarAddress { get; }
        Guid productId { get; }
    }
}
