#nullable enable
using System;
using System.Collections.Immutable;
using System.Numerics;
using Bencodex;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public interface IDelegateeMetadata : IBencodable
    {
        Address DelegateeAddress { get; }

        Address DelegateeAccountAddress { get; }

        Address Address { get; }

        Currency DelegationCurrency { get; }

        Currency RewardCurrency { get; }

        Address DelegationPoolAddress { get; }

        long UnbondingPeriod { get; }

        int MaxUnbondLockInEntries { get; }

        int MaxRebondGraceEntries { get; }

        Address RewardCollectorAddress { get; }

        Address RewardDistributorAddress { get; }

        ImmutableSortedSet<Address> Delegators { get; }

        FungibleAssetValue TotalDelegatedFAV { get; }

        BigInteger TotalShares { get; }

        bool Jailed { get; }

        long JailedUntil { get; }

        bool Tombstoned { get; }

        BigInteger ShareFromFAV(FungibleAssetValue fav);

        FungibleAssetValue FAVFromShare(BigInteger share);
    }
}
