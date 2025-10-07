using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Delegation
{
    public interface IUnbonding
    {
        Address Address { get; }

        Address DelegateeAddress { get; }

        Address DelegatorAddress { get; }

        long LowestExpireHeight { get; }

        bool IsFull { get; }

        bool IsEmpty { get; }

        IUnbonding Release(long height, out FungibleAssetValue? releasedFAV);

        IUnbonding Slash(
            BigInteger slashFactor,
            long infractionHeight,
            long height,
            Address slashedPoolAddress);
    }
}
