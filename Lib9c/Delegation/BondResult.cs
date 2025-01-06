#nullable enable
using System.Numerics;

namespace Nekoyume.Delegation
{
    public readonly struct BondResult
    {
        public BondResult(BigInteger share, Bond bond)
        {
            Share = share;
            Bond = bond;
        }

        public BigInteger Share { get; }

        public Bond Bond { get; }
    }
}
