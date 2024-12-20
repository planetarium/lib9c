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

        /// <summary>
        /// Create a new instance of DelegateeMetadata.
        /// </summary>
        /// <param name="delegateeAddress">
        /// The <see cref="Address"/> of the <see cref="Delegatee{T, TSelf}"/>.
        /// </param>
        /// <param name="delegateeAccountAddress">
        /// The <see cref="Address"/> of the account of the <see cref="Delegatee{T, TSelf}"/>.
        /// </param>
        /// <param name="delegationCurrency">
        /// The <see cref="Currency"/> used for delegation.
        /// </param>
        /// <param name="rewardCurrencies">
        /// The enumerable of <see cref="Currency"/>s used for reward.
        /// </param>
        /// <param name="delegationPoolAddress">
        /// The <see cref="Address"/> of the delegation pool that stores
        /// delegated <see cref="FungibleAssetValue"/>s.
        /// </param>
        /// <param name="rewardPoolAddress">
        /// The <see cref="Address"/> of the reward pool that gathers
        /// rewards to be distributed.
        /// </param>
        /// <param name="rewardRemainderPoolAddress">
        /// The <see cref="Address"/> of the reward remainder pool to
        /// sends the remainder of the rewards to.
        /// </param>
        /// <param name="slashedPoolAddress">
        /// The <see cref="Address"/> of the pool that sends the slashed
        /// <see cref="FungibleAssetValue"/>s to.
        /// </param>
        /// <param name="unbondingPeriod">
        /// The period in blocks that the unbonded <see cref="FungibleAssetValue"/>s
        /// can be withdrawn.
        /// </param>
        /// <param name="maxUnbondLockInEntries">
        /// The maximum number of entries that can be locked in for unbonding.
        /// </param>
        /// <param name="maxRebondGraceEntries">
        /// The maximum number of entries that can be locked in for rebonding.
        /// </param>
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
            Address delegateeAddress,
            Address delegateeAccountAddress,
            List bencoded)
        {
            if (bencoded[0] is not Text text || text != StateTypeName || bencoded[1] is not Integer integer)
            {
                throw new InvalidCastException();
            }

            if (integer > StateVersion)
            {
                throw new FailedLoadStateException("Un-deserializable state.");
            }

            DelegateeAddress = delegateeAddress;
            DelegateeAccountAddress = delegateeAccountAddress;
            DelegationCurrency = new Currency(bencoded[2]);
            RewardCurrencies = ((List)bencoded[3]).Select(v => new Currency(v)).ToImmutableSortedSet();
            DelegationPoolAddress = new Address(bencoded[4]);
            RewardPoolAddress = new Address(bencoded[5]);
            RewardRemainderPoolAddress = new Address(bencoded[6]);
            SlashedPoolAddress = new Address(bencoded[7]);
            UnbondingPeriod = (Integer)bencoded[8];
            MaxUnbondLockInEntries = (Integer)bencoded[9];
            MaxRebondGraceEntries = (Integer)bencoded[10];
            TotalDelegatedFAV = new FungibleAssetValue(bencoded[11]);
            TotalShares = (Integer)bencoded[12];
            Jailed = (Bencodex.Types.Boolean)bencoded[13];
            JailedUntil = (Integer)bencoded[14];
            Tombstoned = (Bencodex.Types.Boolean)bencoded[15];
            UnbondingRefs = ((List)bencoded[16]).Select(item => new UnbondingRef(item)).ToImmutableSortedSet();

            if (!TotalDelegatedFAV.Currency.Equals(DelegationCurrency))
            {
                throw new InvalidOperationException("Invalid currency.");
            }

            if (TotalDelegatedFAV.Sign < 0)
            {
                throw new InvalidOperationException(
                    "Total delegated must be non-negative.");
            }

            if (TotalShares.Sign < 0)
            {
                throw new InvalidOperationException(
                    "Total shares must be non-negative.");
            }
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

        /// <summary>
        /// Get the <see cref="Address"/> of the distribution pool
        /// where the rewards are distributed from.
        /// </summary>
        /// <returns>
        /// <see cref="Address"/> of the distribution pool.
        /// </returns>
        public virtual Address DistributionPoolAddress()
            => DelegationAddress.DistributionPoolAddress(Address);

        /// <summary>
        /// Get the <see cref="Address"/> of the current <see cref="RewardBase"/>.
        /// </summary>
        /// <returns>
        /// <see cref="Address"/> of the current <see cref="RewardBase"/>.
        /// </returns>
        public virtual Address CurrentRewardBaseAddress()
            => DelegationAddress.CurrentRewardBaseAddress(Address);

        /// <summary>
        /// Get the <see cref="Address"/> of the <see cref="RewardBase"/> at the given height.
        /// </summary>
        /// <param name="height"></param>
        /// <returns>
        /// <see cref="Address"/> of the <see cref="RewardBase"/> at the given height.
        /// </returns>
        public virtual Address RewardBaseAddress(long height)
            => DelegationAddress.RewardBaseAddress(Address, height);

        /// <summary>
        /// Get the <see cref="Address"/> of the current lump sum rewards record.
        /// This will be removed after the migration is done.
        /// </summary>
        /// <returns>
        /// <see cref="Address"/> of the current lump sum rewards record.
        /// </returns>
        public virtual Address CurrentLumpSumRewardsRecordAddress()
            => DelegationAddress.CurrentRewardBaseAddress(Address);

        /// <summary>
        /// Get the <see cref="Address"/> of the lump sum rewards record at the given height.
        /// This will be removed after the migration is done.
        /// </summary>
        /// <param name="height">
        /// The height of the lump sum rewards record.
        /// </param>
        /// <returns>
        /// <see cref="Address"/> of the lump sum rewards record at the given height.
        /// </returns>
        public virtual Address LumpSumRewardsRecordAddress(long height)
            => DelegationAddress.RewardBaseAddress(Address, height);

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
