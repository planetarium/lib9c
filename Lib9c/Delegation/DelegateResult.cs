#nullable enable
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public class DelegateResult<T> : IDelegateResult
        where T : IDelegatee
    {
        public DelegateResult(
            T delegatee,
            Bond bond,
            FungibleAssetValue delegatedFAV,
            LumpSumRewardsRecord lumpSumRewardsRecord)
        {
            Delegatee = delegatee;
            Bond = bond;
            DelegatedFAV = delegatedFAV;
            LumpSumRewardsRecord = lumpSumRewardsRecord;
        }

        public T Delegatee { get; }

        IDelegatee IDelegateResult.Delegatee => Delegatee;

        public Bond Bond { get; }

        public FungibleAssetValue DelegatedFAV { get; }

        public LumpSumRewardsRecord LumpSumRewardsRecord { get; }
    }
}
