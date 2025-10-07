#nullable enable
using System.Collections.Immutable;
using System.Numerics;
using Bencodex;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Delegation
{
    public interface IDelegateeMetadata : IBencodable
    {
        Address DelegateeAddress { get; }

        Address DelegateeAccountAddress { get; }

        Address Address { get; }

        Currency DelegationCurrency { get; }

        ImmutableSortedSet<Currency> RewardCurrencies { get; }

        Address DelegationPoolAddress { get; }

        Address RewardRemainderPoolAddress { get; }

        long UnbondingPeriod { get; }

        int MaxUnbondLockInEntries { get; }

        int MaxRebondGraceEntries { get; }

        Address RewardPoolAddress { get; }

        FungibleAssetValue TotalDelegatedFAV { get; }

        BigInteger TotalShares { get; }

        bool Jailed { get; }

        long JailedUntil { get; }

        bool Tombstoned { get; }

        BigInteger ShareFromFAV(FungibleAssetValue fav);

        FungibleAssetValue FAVFromShare(BigInteger share);
    }
}
