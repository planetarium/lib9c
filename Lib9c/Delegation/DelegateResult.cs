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
            FungibleAssetValue delegatedFAV)
        {
            Delegatee = delegatee;
            Bond = bond;
            DelegatedFAV = delegatedFAV;
        }

        public T Delegatee { get; }

        IDelegatee IDelegateResult.Delegatee => Delegatee;

        public Bond Bond { get; }

        public FungibleAssetValue DelegatedFAV { get; }
    }
}
