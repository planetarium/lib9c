using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public interface IUnbonding
    {
        Address Address { get; }

        long LowestExpireHeight { get; }

        bool IsFull { get; }

        bool IsEmpty { get; }

        IUnbonding Release(long height, out FungibleAssetValue? releasedFAV);

        IUnbonding Slash(
            BigInteger slashFactor,
            long infractionHeight,
            long height,
            out FungibleAssetValue? slashedFAV);
    }
}
