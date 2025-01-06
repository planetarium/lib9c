using System.Collections.Generic;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public interface IUnbonding
    {
        Address Address { get; }

        Address DelegateeAddress { get; }

        Address DelegatorAddress { get; }

        long LowestExpireHeight { get; }

        bool IsFull { get; }

        bool IsEmpty { get; }

        UnbondingRef Reference { get; }

        IUnbonding Release(long height, out FungibleAssetValue? releasedFAV);

        IUnbonding Slash(
            BigInteger slashFactor,
            long infractionHeight,
            out SortedDictionary<Address, FungibleAssetValue> slashed);
    }
}
