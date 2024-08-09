using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public class ClaimRewardResult<T> : IClaimRewardResult
        where T: IDelegatee
    {
        public ClaimRewardResult(
            T delegatee, FungibleAssetValue reward, LumpSumRewardsRecord lumpSumRewardsRecord)
        {
            Delegatee = delegatee;
            Reward = reward;
            LumpSumRewardsRecord = lumpSumRewardsRecord;
        }

        public T Delegatee { get; }

        IDelegatee IClaimRewardResult.Delegatee => Delegatee;

        public FungibleAssetValue Reward { get; }

        public LumpSumRewardsRecord LumpSumRewardsRecord { get; }
    }
}
