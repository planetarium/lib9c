#nullable enable
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public interface IDelegateResult
    {
        IDelegatee Delegatee { get; }

        Bond Bond { get; }

        FungibleAssetValue DelegatedFAV { get; }
    }
}
