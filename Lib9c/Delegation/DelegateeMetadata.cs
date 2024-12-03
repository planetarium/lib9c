#nullable enable
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace Nekoyume.Delegation
{
    public class DelegateeMetadata : IDelegateeMetadata
    {
        private const string StateTypeName = "delegatee_metadata";
        private const long StateVersion = 1;

        private Address? _address;
        private readonly IComparer<Currency> _currencyComparer = new CurrencyComparer();

        public DelegateeMetadata(
            Address delegateeAddress,
            Address delegateeAccountAddress,
            Currency delegationCurrency,
            IEnumerable<Currency> rewardCurrencies,
            Address delegationPoolAddress,
            Address rewardPoolAddress,
            Address rewardRemainderPoolAddress,
            Address slashedPoolAddress,
            long unbondingPeriod,
            int maxUnbondLockInEntries,
            int maxRebondGraceEntries)
            : this(
                  delegateeAddress,
                  delegateeAccountAddress,
                  delegationCurrency,
                  rewardCurrencies,
                  delegationPoolAddress,
                  rewardPoolAddress,
                  rewardRemainderPoolAddress,
                  slashedPoolAddress,
                  unbondingPeriod,
                  maxUnbondLockInEntries,
                  maxRebondGraceEntries,
                  delegationCurrency * 0,
                  BigInteger.Zero,
                  false,
                  -1L,
                  false,
                  ImmutableSortedSet<UnbondingRef>.Empty)
        {   
        }

        public DelegateeMetadata(
            Address delegateeAddress,
            Address delegateeAccountAddress,
            IValue bencoded)
            : this(delegateeAddress, delegateeAccountAddress, (List)bencoded)
        {
        }

        public DelegateeMetadata(
            Address address,
            Address accountAddress,
            List bencoded)
        {
            Currency delegationCurrency;
            IEnumerable< Currency > rewardCurrencies;
            Address delegationPoolAddress;
            Address rewardPoolAddress;
            Address rewardRemainderPoolAddress;
            Address slashedPoolAddress;
            long unbondingPeriod;
            int maxUnbondLockInEntries;
            int maxRebondGraceEntries;
            FungibleAssetValue totalDelegated;
            BigInteger totalShares;
            bool jailed;
            long jailedUntil;
            bool tombstoned;
            IEnumerable<UnbondingRef> unbondingRefs;

            // TODO: Remove this if block after migration to state version 1 is done.
            if (bencoded[0] is not Text)
            {
                // Assume state version 0
                delegationCurrency = new Currency(bencoded[0]);
                rewardCurrencies = ((List)bencoded[1]).Select(v => new Currency(v));
                delegationPoolAddress = new Address(bencoded[2]);
                rewardPoolAddress = new Address(bencoded[3]);
                rewardRemainderPoolAddress = new Address(bencoded[4]);
                slashedPoolAddress = new Address(bencoded[5]);
                unbondingPeriod = (Integer)bencoded[6];
                maxUnbondLockInEntries = (Integer)bencoded[7];
                maxRebondGraceEntries = (Integer)bencoded[8];
                totalDelegated = new FungibleAssetValue(bencoded[10]);
                totalShares = (Integer)bencoded[11];
                jailed = (Bencodex.Types.Boolean)bencoded[12];
                jailedUntil = (Integer)bencoded[13];
                tombstoned = (Bencodex.Types.Boolean)bencoded[14];
                unbondingRefs = ((List)bencoded[15]).Select(item => new UnbondingRef(item));
            }
            else
            {
                if (bencoded[0] is not Text text || text != StateTypeName || bencoded[1] is not Integer integer)
                {
                    throw new InvalidCastException();
                }

                if (integer > StateVersion)
                {
                    throw new FailedLoadStateException("Un-deserializable state.");
                }

                delegationCurrency = new Currency(bencoded[2]);
                rewardCurrencies = ((List)bencoded[3]).Select(v => new Currency(v));
                delegationPoolAddress = new Address(bencoded[4]);
                rewardPoolAddress = new Address(bencoded[5]);
                rewardRemainderPoolAddress = new Address(bencoded[6]);
                slashedPoolAddress = new Address(bencoded[7]);
                unbondingPeriod = (Integer)bencoded[8];
                maxUnbondLockInEntries = (Integer)bencoded[9];
                maxRebondGraceEntries = (Integer)bencoded[10];
                totalDelegated = new FungibleAssetValue(bencoded[11]);
                totalShares = (Integer)bencoded[12];
                jailed = (Bencodex.Types.Boolean)bencoded[13];
                jailedUntil = (Integer)bencoded[14];
                tombstoned = (Bencodex.Types.Boolean)bencoded[15];
                unbondingRefs = ((List)bencoded[16]).Select(item => new UnbondingRef(item));
            }

            if (!totalDelegated.Currency.Equals(delegationCurrency))
            {
                throw new InvalidOperationException("Invalid currency.");
            }

            if (totalDelegated.Sign < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalDelegated),
                    totalDelegated,
                    "Total delegated must be non-negative.");
            }

            if (totalShares.Sign < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalShares),
                    totalShares,
                    "Total shares must be non-negative.");
            }

            DelegateeAddress = address;
            DelegateeAccountAddress = accountAddress;
            DelegationCurrency = delegationCurrency;
            RewardCurrencies = rewardCurrencies.ToImmutableSortedSet(_currencyComparer);
            DelegationPoolAddress = delegationPoolAddress;
            RewardPoolAddress = rewardPoolAddress;
            RewardRemainderPoolAddress = rewardRemainderPoolAddress;
            SlashedPoolAddress = slashedPoolAddress;
            UnbondingPeriod = unbondingPeriod;
            MaxUnbondLockInEntries = maxUnbondLockInEntries;
            MaxRebondGraceEntries = maxRebondGraceEntries;
            TotalDelegatedFAV = totalDelegated;
            TotalShares = totalShares;
            Jailed = jailed;
            JailedUntil = jailedUntil;
            Tombstoned = tombstoned;
            UnbondingRefs = unbondingRefs.ToImmutableSortedSet();
        }

        private DelegateeMetadata(
            Address delegateeAddress,
            Address delegateeAccountAddress,
            Currency delegationCurrency,
            IEnumerable<Currency> rewardCurrencies,
            Address delegationPoolAddress,
            Address rewardPoolAddress,
            Address rewardRemainderPoolAddress,
            Address slashedPoolAddress,
            long unbondingPeriod,
            int maxUnbondLockInEntries,
            int maxRebondGraceEntries,
            FungibleAssetValue totalDelegated,
            BigInteger totalShares,
            bool jailed,
            long jailedUntil,
            bool tombstoned,
            IEnumerable<UnbondingRef> unbondingRefs)
        {
            if (!totalDelegated.Currency.Equals(delegationCurrency))
            {
                throw new InvalidOperationException("Invalid currency.");
            }

            if (totalDelegated.Sign < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalDelegated),
                    totalDelegated,
                    "Total delegated must be non-negative.");
            }

            if (totalShares.Sign < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalShares),
                    totalShares,
                    "Total shares must be non-negative.");
            }

            DelegateeAddress = delegateeAddress;
            DelegateeAccountAddress = delegateeAccountAddress;
            DelegationCurrency = delegationCurrency;
            RewardCurrencies = rewardCurrencies.ToImmutableSortedSet(_currencyComparer);
            DelegationPoolAddress = delegationPoolAddress;
            RewardPoolAddress = rewardPoolAddress;
            RewardRemainderPoolAddress = rewardRemainderPoolAddress;
            SlashedPoolAddress = slashedPoolAddress;
            UnbondingPeriod = unbondingPeriod;
            MaxUnbondLockInEntries = maxUnbondLockInEntries;
            MaxRebondGraceEntries = maxRebondGraceEntries;
            TotalDelegatedFAV = totalDelegated;
            TotalShares = totalShares;
            Jailed = jailed;
            JailedUntil = jailedUntil;
            Tombstoned = tombstoned;
            UnbondingRefs = unbondingRefs.ToImmutableSortedSet();
        }

        public Address DelegateeAddress { get; }

        public Address DelegateeAccountAddress { get; }

        public Address Address
            => _address ??= DelegationAddress.DelegateeMetadataAddress(
                DelegateeAddress,
                DelegateeAccountAddress);

        public Currency DelegationCurrency { get; }

        public ImmutableSortedSet<Currency> RewardCurrencies { get; }

        public Address DelegationPoolAddress { get; internal set; }

        public Address RewardPoolAddress { get; }

        public Address RewardRemainderPoolAddress { get; }

        public Address SlashedPoolAddress { get; }

        public long UnbondingPeriod { get; private set; }

        public int MaxUnbondLockInEntries { get; }

        public int MaxRebondGraceEntries { get; }

        public FungibleAssetValue TotalDelegatedFAV { get; private set; }

        public BigInteger TotalShares { get; private set; }

        public bool Jailed { get; internal set; }

        public long JailedUntil { get; internal set; }

        public bool Tombstoned { get; internal set; }

        public ImmutableSortedSet<UnbondingRef> UnbondingRefs { get; private set; }

        // TODO : Better serialization
        public List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(DelegationCurrency.Serialize())
            .Add(new List(RewardCurrencies.Select(c => c.Serialize())))
            .Add(DelegationPoolAddress.Bencoded)
            .Add(RewardPoolAddress.Bencoded)
            .Add(RewardRemainderPoolAddress.Bencoded)
            .Add(SlashedPoolAddress.Bencoded)
            .Add(UnbondingPeriod)
            .Add(MaxUnbondLockInEntries)
            .Add(MaxRebondGraceEntries)
            .Add(TotalDelegatedFAV.Serialize())
            .Add(TotalShares)
            .Add(Jailed)
            .Add(JailedUntil)
            .Add(Tombstoned)
            .Add(new List(UnbondingRefs.Select(unbondingRef => unbondingRef.Bencoded)));

        IValue IBencodable.Bencoded => Bencoded;

        public BigInteger ShareFromFAV(FungibleAssetValue fav)
            => TotalShares.IsZero
                ? fav.RawValue
                : TotalShares * fav.RawValue / TotalDelegatedFAV.RawValue;

        public FungibleAssetValue FAVFromShare(BigInteger share)
            => TotalShares == share
                ? TotalDelegatedFAV
                : (TotalDelegatedFAV * share).DivRem(TotalShares).Quotient;

        public void AddDelegatedFAV(FungibleAssetValue fav)
        {
            TotalDelegatedFAV += fav;
        }

        public void RemoveDelegatedFAV(FungibleAssetValue fav)
        {
            TotalDelegatedFAV -= fav;
        }

        public void AddShare(BigInteger share)
        {
            TotalShares += share;
        }

        public void RemoveShare(BigInteger share)
        {
            TotalShares -= share;
        }

        public void AddUnbondingRef(UnbondingRef unbondingRef)
        {
            UnbondingRefs = UnbondingRefs.Add(unbondingRef);
        }

        public void RemoveUnbondingRef(UnbondingRef unbondingRef)
        {
            UnbondingRefs = UnbondingRefs.Remove(unbondingRef);
        }

        public Address BondAddress(Address delegatorAddress)
            => DelegationAddress.BondAddress(Address, delegatorAddress);

        public Address UnbondLockInAddress(Address delegatorAddress)
            => DelegationAddress.UnbondLockInAddress(Address, delegatorAddress);

        public virtual Address RebondGraceAddress(Address delegatorAddress)
            => DelegationAddress.RebondGraceAddress(Address, delegatorAddress);

        public virtual Address CurrentLumpSumRewardsRecordAddress()
            => DelegationAddress.CurrentLumpSumRewardsRecordAddress(Address);

        public virtual Address LumpSumRewardsRecordAddress(long height)
            => DelegationAddress.LumpSumRewardsRecordAddress(Address, height);

        public override bool Equals(object? obj)
            => obj is IDelegateeMetadata other && Equals(other);

        public virtual bool Equals(IDelegateeMetadata? other)
            => ReferenceEquals(this, other)
            || (other is DelegateeMetadata delegatee
            && (GetType() != delegatee.GetType())
            && DelegateeAddress.Equals(delegatee.DelegateeAddress)
            && DelegateeAccountAddress.Equals(delegatee.DelegateeAccountAddress)
            && DelegationCurrency.Equals(delegatee.DelegationCurrency)
            && RewardCurrencies.SequenceEqual(delegatee.RewardCurrencies)
            && DelegationPoolAddress.Equals(delegatee.DelegationPoolAddress)
            && RewardPoolAddress.Equals(delegatee.RewardPoolAddress)
            && RewardRemainderPoolAddress.Equals(delegatee.RewardRemainderPoolAddress)
            && SlashedPoolAddress.Equals(delegatee.SlashedPoolAddress)
            && UnbondingPeriod == delegatee.UnbondingPeriod
            && RewardPoolAddress.Equals(delegatee.RewardPoolAddress)
            && TotalDelegatedFAV.Equals(delegatee.TotalDelegatedFAV)
            && TotalShares.Equals(delegatee.TotalShares)
            && Jailed == delegatee.Jailed
            && UnbondingRefs.SequenceEqual(delegatee.UnbondingRefs));

        public override int GetHashCode()
            => DelegateeAddress.GetHashCode();

        // TODO: [GuildMigration] Remove this method when the migration is done.
        // Remove private setter for UnbondingPeriod.
        public void UpdateUnbondingPeriod(long unbondingPeriod)
        {
            UnbondingPeriod = unbondingPeriod;
        }

    }
}
