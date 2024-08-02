using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public class DelegateResult
    {
        public DelegateResult(
            IDelegatee delegatee,
            Bond bond,
            FungibleAssetValue delegatedFAV)
        {
            Delegatee = delegatee;
            Bond = bond;
            DelegatedFAV = delegatedFAV;
        }

        IDelegatee Delegatee { get; }

        Bond Bond { get; }

        FungibleAssetValue DelegatedFAV { get; }
    }
}
