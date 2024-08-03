using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public class DelegateResult<T> : IDelegateResult
        where T : IDelegatee
    {
        public DelegateResult(
            T delegatee,
            Bond bond,
            FungibleAssetValue delegatedFAV)
        {
            Delegatee = delegatee;
            Bond = bond;
            DelegatedFAV = delegatedFAV;
        }

        IDelegatee IDelegateResult.Delegatee => Delegatee;

        public T Delegatee { get; }

        public Bond Bond { get; }

        public FungibleAssetValue DelegatedFAV { get; }
    }
}
