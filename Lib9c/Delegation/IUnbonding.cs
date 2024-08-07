using Libplanet.Crypto;

namespace Nekoyume.Delegation
{
    public interface IUnbonding
    {
        Address Address { get; }

        long LowestExpireHeight { get; }

        bool IsFull { get; }

        bool IsEmpty { get; }

        IUnbonding Release(long height);

        IUnbonding Slash();
    }
}
