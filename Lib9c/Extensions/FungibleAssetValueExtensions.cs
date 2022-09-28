using System.Linq;
using Libplanet.Assets;

namespace Nekoyume.Extensions
{
    public static class FungibleAssetValueExtensions
    {
        public static bool IsAvailableInMarket(
            this FungibleAssetValue fav,
            params Currency[] availableCurrencies) =>
            availableCurrencies.Contains(fav.Currency) &&
            fav.MinorUnit.IsZero &&
            fav.Sign >= 0;
    }
}
