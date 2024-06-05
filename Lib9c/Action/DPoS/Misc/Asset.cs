using System;
using System.Collections.Immutable;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Action.DPoS.Misc
{
    public struct Asset
    {
        public static readonly Currency ConsensusToken =
            Currency.Uncapped("ConsensusToken", 0, minters: null);

        public static readonly Currency Share =
            Currency.Uncapped("Share", 0, minters: null);

        public static FungibleAssetValue ConsensusFromGovernance(FungibleAssetValue governanceTokens) =>
            ConvertTokens(governanceTokens, ConsensusToken);

        public static FungibleAssetValue ConvertTokens(FungibleAssetValue sourceTokens, Currency targetToken)
        {
            if (sourceTokens.Currency.Equals(targetToken))
            {
                throw new ArgumentException(
                    $"Target currency {targetToken} cannot be the same as the source asset's currency {sourceTokens.Currency}.",
                    nameof(targetToken));
            }

            return FungibleAssetValue.FromRawValue(targetToken, sourceTokens.RawValue);
        }
    }
}
