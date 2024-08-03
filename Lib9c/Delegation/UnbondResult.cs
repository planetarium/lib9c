using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public class UnbondResult
    {
        public UnbondResult(Bond bond, FungibleAssetValue unbondedFAV)
        {
            Bond = bond;

            UnbondedFAV = unbondedFAV;
        }

        public Bond Bond { get; }

        public FungibleAssetValue UnbondedFAV { get; }
    }
}
