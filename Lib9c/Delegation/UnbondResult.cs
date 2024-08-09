#nullable enable
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public class UnbondResult
    {
        public UnbondResult(Bond bond, FungibleAssetValue unbondedFAV, LumpSumRewardsRecord lumpSumRewardsRecord)
        {
            Bond = bond;
            UnbondedFAV = unbondedFAV;
            LumpSumRewardsRecord = lumpSumRewardsRecord;
        }

        public Bond Bond { get; }

        public FungibleAssetValue UnbondedFAV { get; }

        public LumpSumRewardsRecord LumpSumRewardsRecord { get; }
    }
}
