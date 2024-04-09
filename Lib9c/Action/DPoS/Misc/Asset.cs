using System.Numerics;
using Libplanet.Types.Assets;

namespace Nekoyume.Action.DPoS.Misc
{
    public struct Asset
    {
        public static readonly Currency GovernanceToken =
            Currency.Legacy("NCG", 2, null);

        public static readonly Currency ConsensusToken =
            Currency.Uncapped("ConsensusToken", 0, minters: null);

        public static readonly Currency Share =
            Currency.Uncapped("Share", 0, minters: null);

        public static FungibleAssetValue ConsensusFromGovernance(FungibleAssetValue governanceToken)
            => FungibleAssetValue.FromRawValue(ConsensusToken, governanceToken.RawValue);

        public static FungibleAssetValue ConsensusFromGovernance(BigInteger amount)
            => ConsensusFromGovernance(GovernanceToken * amount);

        public static FungibleAssetValue GovernanceFromConsensus(FungibleAssetValue consensusToken)
            => FungibleAssetValue.FromRawValue(GovernanceToken, consensusToken.RawValue);
    }
}
