using System;
using System.Collections.Immutable;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Action.DPoS.Misc
{
    public struct Asset
    {
        public static readonly Currency GovernanceToken =
            Currency.Legacy(
                "NCG",
                2,
                new[] { new Address("47d082a115c63e7b58b1532d20e631538eafadde") }
                    .ToImmutableHashSet());

        public static readonly Currency ConsensusToken =
            Currency.Uncapped("ConsensusToken", 0, minters: null);

        public static readonly Currency Share =
            Currency.Uncapped("Share", 0, minters: null);

        public static FungibleAssetValue ConsensusFromGovernance(FungibleAssetValue governanceToken)
        {
            if (!governanceToken.Currency.Equals(GovernanceToken))
            {
                throw new ArgumentException(
                    message: $"'{governanceToken}' is not {nameof(GovernanceToken)}",
                    paramName: nameof(governanceToken));
            }

            return FungibleAssetValue.FromRawValue(ConsensusToken, governanceToken.RawValue);
        }

        public static FungibleAssetValue ConsensusFromGovernance(BigInteger amount)
            => ConsensusFromGovernance(GovernanceToken * amount);

        public static FungibleAssetValue GovernanceFromConsensus(FungibleAssetValue consensusToken)
        {
            if (!consensusToken.Currency.Equals(ConsensusToken))
            {
                throw new ArgumentException(
                    message: $"'{consensusToken}' is not {nameof(ConsensusToken)}",
                    paramName: nameof(consensusToken));
            }

            return FungibleAssetValue.FromRawValue(GovernanceToken, consensusToken.RawValue);
        }
    }
}
