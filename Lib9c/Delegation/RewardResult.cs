using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public class RewardResult
    {
        public RewardResult(FungibleAssetValue reward, LumpSumRewardsRecord lumpSumRewardsRecord)
        {
            Reward = reward;
            LumpSumRewardsRecord = lumpSumRewardsRecord;
        }

        public FungibleAssetValue Reward { get; }

        public LumpSumRewardsRecord LumpSumRewardsRecord { get; }
    }
}
