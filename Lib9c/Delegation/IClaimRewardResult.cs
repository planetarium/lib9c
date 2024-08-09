using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public interface IClaimRewardResult
    {
        IDelegatee Delegatee { get; }

        FungibleAssetValue Reward { get; }

        LumpSumRewardsRecord LumpSumRewardsRecord { get; }
    }
}
