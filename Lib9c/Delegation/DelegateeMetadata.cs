#nullable enable
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace Nekoyume.Delegation
{
    public class DelegateeMetadata : IDelegateeMetadata
    {
        private Address? _address;

        public DelegateeMetadata(
            Address delegateeAddress,
            Address delegateeAccountAddress,
            Currency delegationCurrency,
            Currency rewardCurrency,
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
                  rewardCurrency,
                  delegationPoolAddress,
                  rewardPoolAddress,
                  rewardRemainderPoolAddress,
                  slashedPoolAddress,
                  unbondingPeriod,
                  maxUnbondLockInEntries,
                  maxRebondGraceEntries,
                  ImmutableSortedSet<Address>.Empty,
                  delegationCurrency * 0,
                  BigInteger.Zero,
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
            : this(
                  address,
                  accountAddress,
                  new Currency(bencoded[0]),
                  new Currency(bencoded[1]),
                  new Address(bencoded[2]),
                  new Address(bencoded[3]),
                  new Address(bencoded[4]),
                  new Address(bencoded[5]),
                  (Integer)bencoded[6],
                  (Integer)bencoded[7],
                  (Integer)bencoded[8],
                  ((List)bencoded[9]).Select(item => new Address(item)),
                  new FungibleAssetValue(bencoded[10]),
                  (Integer)bencoded[11],
                  ((List)bencoded[12]).Select(item => new UnbondingRef(item)))
        {
        }

        private DelegateeMetadata(
            Address delegateeAddress,
            Address delegateeAccountAddress,
            Currency delegationCurrency,
            Currency rewardCurrency,
            Address delegationPoolAddress,
            Address rewardPoolAddress,
            Address rewardRemainderPoolAddress,
            Address slashedPoolAddress,
            long unbondingPeriod,
            int maxUnbondLockInEntries,
            int maxRebondGraceEntries,
            IEnumerable<Address> delegators,
            FungibleAssetValue totalDelegated,
            BigInteger totalShares,
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
            RewardCurrency = rewardCurrency;
            DelegationPoolAddress = delegationPoolAddress;
            RewardPoolAddress = rewardPoolAddress;
            RewardRemainderPoolAddress = rewardRemainderPoolAddress;
            SlashedPoolAddress = slashedPoolAddress;
            UnbondingPeriod = unbondingPeriod;
            MaxUnbondLockInEntries = maxUnbondLockInEntries;
            MaxRebondGraceEntries = maxRebondGraceEntries;
            Delegators = delegators.ToImmutableSortedSet();
            TotalDelegatedFAV = totalDelegated;
            TotalShares = totalShares;
            UnbondingRefs = unbondingRefs.ToImmutableSortedSet();
        }

        public Address DelegateeAddress { get; }

        public Address DelegateeAccountAddress { get; }

        public Address Address
            => _address ??= DelegationAddress.DelegateeMetadataAddress(
                DelegateeAddress,
                DelegateeAccountAddress);

        public Currency DelegationCurrency { get; }

        public Currency RewardCurrency { get; }

        public Address DelegationPoolAddress { get; }

        public Address RewardPoolAddress { get; }

        public Address RewardRemainderPoolAddress { get; }

        public Address SlashedPoolAddress { get; }

        public long UnbondingPeriod { get; private set; }

        public int MaxUnbondLockInEntries { get; }

        public int MaxRebondGraceEntries { get; }

        public ImmutableSortedSet<Address> Delegators { get; private set; }

        public FungibleAssetValue TotalDelegatedFAV { get; private set; }

        public BigInteger TotalShares { get; private set; }

        public ImmutableSortedSet<UnbondingRef> UnbondingRefs { get; private set; }

        public List Bencoded => List.Empty
            .Add(DelegationCurrency.Serialize())
            .Add(RewardCurrency.Serialize())
            .Add(DelegationPoolAddress.Bencoded)
            .Add(RewardPoolAddress.Bencoded)
            .Add(RewardRemainderPoolAddress.Bencoded)
            .Add(SlashedPoolAddress.Bencoded)
            .Add(UnbondingPeriod)
            .Add(MaxUnbondLockInEntries)
            .Add(MaxRebondGraceEntries)
            .Add(new List(Delegators.Select(delegator => delegator.Bencoded)))
            .Add(TotalDelegatedFAV.Serialize())
            .Add(TotalShares)
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

        public void AddDelegator(Address delegatorAddress)
        {
            Delegators = Delegators.Add(delegatorAddress);
        }

        public void RemoveDelegator(Address delegatorAddress)
        {
            Delegators = Delegators.Remove(delegatorAddress);
        }

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
            && RewardCurrency.Equals(delegatee.RewardCurrency)
            && DelegationPoolAddress.Equals(delegatee.DelegationPoolAddress)
            && RewardPoolAddress.Equals(delegatee.RewardPoolAddress)
            && RewardRemainderPoolAddress.Equals(delegatee.RewardRemainderPoolAddress)
            && SlashedPoolAddress.Equals(delegatee.SlashedPoolAddress)
            && UnbondingPeriod == delegatee.UnbondingPeriod
            && RewardPoolAddress.Equals(delegatee.RewardPoolAddress)
            && Delegators.SequenceEqual(delegatee.Delegators)
            && TotalDelegatedFAV.Equals(delegatee.TotalDelegatedFAV)
            && TotalShares.Equals(delegatee.TotalShares)
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
