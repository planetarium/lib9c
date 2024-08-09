#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegatee<T, TSelf> : IDelegatee
        where T : Delegator<TSelf, T>
        where TSelf : Delegatee<T, TSelf>
    {
        protected readonly byte[] BondId = new byte[] { 0x44 };                  // `D`
        protected readonly byte[] UnbondLockInId = new byte[] { 0x55 };          // `U`
        protected readonly byte[] RebondGraceId = new byte[] { 0x52 };           // `R`
        protected readonly byte[] LumpSumRewardsRecordId = new byte[] { 0x4c };  // `L`
        protected readonly byte[] RewardPoolId = new byte[] { 0x72 };            // `r`
        protected readonly byte[] PoolId = new byte[] { 0x70 };                  // `p`

        public Delegatee(Address address)
        {
            Address = address;
            Delegators = ImmutableSortedSet<Address>.Empty;
            TotalDelegated = Currency * 0;
            TotalShares = BigInteger.Zero;
            LastRewardPeriodStartHeight = -1;
        }

        public Delegatee(Address address, IValue bencoded)
            : this(address, (List)bencoded)
        {
        }

        public Delegatee(Address address, List bencoded)
            : this(
                  address,
                  ((List)bencoded[0]).Select(item => new Address(item)),
                  new FungibleAssetValue(bencoded[1]),
                  (Integer)bencoded[2],
                  (Integer)bencoded[3])
        {
        }

        private Delegatee(
            Address address,
            IEnumerable<Address> delegators,
            FungibleAssetValue totalDelegated,
            BigInteger totalShares,
            long lastRewardPeriodStartHeight)
        {
            if (!totalDelegated.Currency.Equals(Currency))
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

            Address = address;
            Delegators = delegators.ToImmutableSortedSet();
            TotalDelegated = totalDelegated;
            TotalShares = totalShares;
            LastRewardPeriodStartHeight = lastRewardPeriodStartHeight;
        }

        public Address Address { get; }

        public abstract Currency Currency { get; }

        public abstract Currency RewardCurrency { get; }

        public abstract Address PoolAddress { get; }

        public abstract long UnbondingPeriod { get; }

        public abstract byte[] DelegateeId { get; }

        public abstract int MaxUnbondLockInEntries { get; }

        public abstract int MaxRebondGraceEntries { get; }

        public Address RewardPoolAddress => DeriveAddress(RewardPoolId);

        public ImmutableSortedSet<Address> Delegators { get; private set; }

        public FungibleAssetValue TotalDelegated { get; private set; }

        public BigInteger TotalShares { get; private set; }

        public long LastRewardPeriodStartHeight { get; private set; }

        public List Bencoded => List.Empty
            .Add(new List(Delegators.Select(delegator => delegator.Bencoded)))
            .Add(TotalDelegated.Serialize())
            .Add(TotalShares)
            .Add(LastRewardPeriodStartHeight);

        IValue IBencodable.Bencoded => Bencoded;

        public BigInteger ShareToBond(FungibleAssetValue fav)
            => TotalShares.IsZero
                ? fav.RawValue
                : TotalShares * fav.RawValue / TotalDelegated.RawValue;

        public FungibleAssetValue FAVToUnbond(BigInteger share)
            => TotalShares == share
                ? TotalDelegated
                : (TotalDelegated * share).DivRem(TotalShares, out _);

        BondResult IDelegatee.Bond(
            IDelegator delegator, FungibleAssetValue fav, long height, Bond bond)
            => Bond((T)delegator, fav, height, bond);

        UnbondResult IDelegatee.Unbond(
            IDelegator delegator, BigInteger share, long height, Bond bond)
            => Unbond((T)delegator, share, height, bond);

        public BondResult Bond(T delegator, FungibleAssetValue fav, long height, Bond bond)
        {
            if (!fav.Currency.Equals(Currency))
            {
                throw new InvalidOperationException(
                    "Cannot bond with invalid currency.");
            }

            BigInteger share = ShareToBond(fav);
            bond = bond.AddShare(share);
            Delegators = Delegators.Add(delegator.Address);
            TotalShares += share;
            TotalDelegated += fav;
            var lumpSumRewardsRecord = StartNewRewardPeriod(LastRewardPeriodStartHeight);
            LastRewardPeriodStartHeight = height;
            
            return new BondResult(
                bond, share, lumpSumRewardsRecord);
        }

        public UnbondResult Unbond(T delegator, BigInteger share, long height, Bond bond)
        {
            if (TotalShares.IsZero || TotalDelegated.RawValue.IsZero)
            {
                throw new InvalidOperationException(
                    "Cannot unbond without bonding.");
            }

            FungibleAssetValue fav = FAVToUnbond(share);
            bond = bond.SubtractShare(share);
            if (bond.Share.IsZero)
            {
                Delegators = Delegators.Remove(delegator.Address);
            }

            TotalShares -= share;
            TotalDelegated -= fav;
            var lumpSumRewardsRecord = StartNewRewardPeriod(LastRewardPeriodStartHeight);
            LastRewardPeriodStartHeight = height;

            return new UnbondResult(
                bond, fav, lumpSumRewardsRecord);
        }

        public RewardResult Reward(
            BigInteger share,
            long height,
            IEnumerable<LumpSumRewardsRecord> lumpSumRewardsRecords)
        {
            FungibleAssetValue reward = RewardCurrency * 0;
            long? linkedStartHeight = null;

            foreach (LumpSumRewardsRecord record in lumpSumRewardsRecords)
            {
                if (!(record.StartHeight is long startHeight))
                {
                    throw new ArgumentException("lump sum reward record wasn't started.");
                }

                if (linkedStartHeight is long startHeightFromHigher
                    && startHeightFromHigher != startHeight)
                {
                    throw new ArgumentException("lump sum reward record was started.");
                }

                reward += record.RewardsDuringPeriod(share);
                linkedStartHeight = record.LastStartHeight;

                if (linkedStartHeight == -1)
                {
                    break;
                }
            }

            var lumpSumRewardsRecord = StartNewRewardPeriod(LastRewardPeriodStartHeight);
            LastRewardPeriodStartHeight = height;

            return new RewardResult(reward, lumpSumRewardsRecord);
        }

        public Address BondAddress(Address delegatorAddress)
            => DeriveAddress(BondId, delegatorAddress);

        public Address UnbondLockInAddress(Address delegatorAddress)
            => DeriveAddress(UnbondLockInId, delegatorAddress);

        public Address RebondGraceAddress(Address delegatorAddress)
            => DeriveAddress(RebondGraceId, delegatorAddress);

        public Address LumpSumRewardsRecordAddress(long? height = null)
            => height is long heightValue
                ? DeriveAddress(LumpSumRewardsRecordId, BitConverter.GetBytes(heightValue))
                : DeriveAddress(LumpSumRewardsRecordId);

        public override bool Equals(object? obj)
            => obj is IDelegatee other && Equals(other);

        public bool Equals(IDelegatee? other)
            => ReferenceEquals(this, other)
            || (other is Delegatee<T, TSelf> delegatee
            && (GetType() != delegatee.GetType())
            && Address.Equals(delegatee.Address)
            && Currency.Equals(delegatee.Currency)
            && PoolAddress.Equals(delegatee.PoolAddress)
            && UnbondingPeriod == delegatee.UnbondingPeriod
            && RewardPoolAddress.Equals(delegatee.RewardPoolAddress)
            && Delegators.SequenceEqual(delegatee.Delegators)
            && TotalDelegated.Equals(delegatee.TotalDelegated)
            && TotalShares.Equals(delegatee.TotalShares)
            && LastRewardPeriodStartHeight.Equals(delegatee.LastRewardPeriodStartHeight)
            && DelegateeId.SequenceEqual(delegatee.DelegateeId));

        public override int GetHashCode()
            => Address.GetHashCode();

        protected Address DeriveAddress(byte[] typeId, Address address)
            => DeriveAddress(typeId, address.ByteArray);

        protected Address DeriveAddress(byte[] typeId, IEnumerable<byte>? bytes = null)
        {
            byte[] hashed;
            using (var hmac = new HMACSHA1(DelegateeId.Concat(typeId).ToArray()))
            {
                hashed = hmac.ComputeHash(
                    Address.ByteArray.Concat(bytes ?? Array.Empty<byte>()).ToArray());
            }

            return new Address(hashed);
        }

        private LumpSumRewardsRecord StartNewRewardPeriod(long lastStartHeight = -1)
        {
            return new LumpSumRewardsRecord(
                LumpSumRewardsRecordAddress(),
                RewardCurrency,
                TotalShares,
                lastStartHeight);
        }
    }
}
