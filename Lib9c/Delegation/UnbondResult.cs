#nullable enable
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public readonly struct UnbondResult
    {
        public UnbondResult(FungibleAssetValue fav, Bond bond)
        {
            Fav = fav;
            Bond = bond;
        }

        public FungibleAssetValue Fav { get; }

        public Bond Bond { get; }
    }
}
