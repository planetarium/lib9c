#nullable enable
using System.Numerics;

namespace Nekoyume.Delegation
{
    public class BondResult
    {
        public BondResult(Bond bond, BigInteger bondedShare, LumpSumRewardsRecord lumpSumRewardsRecord)
        {
            Bond = bond;
            BondedShare = bondedShare;
            LumpSumRewardsRecord = lumpSumRewardsRecord;
        }

        public Bond Bond { get; }

        public BigInteger BondedShare { get; }

        public LumpSumRewardsRecord LumpSumRewardsRecord { get; }
    }
}
