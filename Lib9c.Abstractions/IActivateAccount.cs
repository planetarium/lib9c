using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IActivateAccount
    {
        Address PendingAddress { get; }
        byte[] Signature { get; }
    }
}
