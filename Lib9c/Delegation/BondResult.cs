using System.Numerics;

namespace Nekoyume.Delegation
{
    public class BondResult
    {
        public BondResult(Bond bond, BigInteger bondedShare)
        {
            Bond = bond;

            BondedShare = bondedShare;
        }

        public Bond Bond { get; }

        public BigInteger BondedShare { get; }
    }
}
