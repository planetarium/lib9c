using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegatee<T, TSelf> : IDelegatee
        where T : Delegator<TSelf, T>
        where TSelf : Delegatee<T, TSelf>
    {
        protected readonly byte[] BondId = new byte[] { 0x44 };          // `D`
        protected readonly byte[] UnbondLockInId = new byte[] { 0x55 };  // `U`
        protected readonly byte[] RebondGraceId = new byte[] { 0x52 };   // `R`
        protected readonly byte[] RewardPoolId = new byte[] { 0x72 };    // `r`
        protected readonly byte[] PoolId = new byte[] { 0x70 };          // `p`

        public Delegatee(Address address)
        {
            Address = address;
            Delegators = ImmutableSortedSet<Address>.Empty;
            TotalDelegated = Currency * 0;
            TotalShares = BigInteger.Zero;
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
                  (Integer)bencoded[2])
        {
        }

        private Delegatee(
            Address address,
            IEnumerable<Address> delegators,
            FungibleAssetValue totalDelegated,
            BigInteger totalShares)
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
        }

        public Address Address { get; }

        public abstract Currency Currency { get; }

        public abstract Address PoolAddress { get; }

        public abstract long UnbondingPeriod { get; }

        public abstract byte[] DelegateeId { get; }

        public Address RewardPoolAddress => DeriveAddress(RewardPoolId);

        public ImmutableSortedSet<Address> Delegators { get; private set; }

        public FungibleAssetValue TotalDelegated { get; private set; }

        public BigInteger TotalShares { get; private set; }

        public IValue Bencoded => List.Empty
            .Add(new List(Delegators.Select(delegator => delegator.Bencoded)))
            .Add(TotalDelegated.Serialize())
            .Add(TotalShares);

        public BigInteger ShareToBond(FungibleAssetValue fav)
            => TotalShares.IsZero
                ? fav.RawValue
                : TotalShares * fav.RawValue / TotalDelegated.RawValue;

        public FungibleAssetValue FAVToUnbond(BigInteger share)
            => TotalShares == share
                ? TotalDelegated
                : (TotalDelegated * share).DivRem(TotalShares, out _);

        void IDelegatee.Bond(IDelegator delegator, FungibleAssetValue fav, Delegation delegation)
            => Bond((T)delegator, fav, delegation);

        void IDelegatee.Unbond(IDelegator delegator, BigInteger share, Delegation delegation)
            => Unbond((T)delegator, share, delegation);

        public void Bond(T delegator, FungibleAssetValue fav, Delegation delegation)
        {
            if (!fav.Currency.Equals(Currency))
            {
                throw new InvalidOperationException("Cannot bond with invalid currency.");
            }

            BigInteger share = ShareToBond(fav);
            delegation.AddBond(fav, share);
            Delegators = Delegators.Add(delegator.Address);
            TotalShares += share;
            TotalDelegated += fav;
            Distribute();
        }

        public void Unbond(T delegator, BigInteger share, Delegation delegation)
        {
            if (TotalShares.IsZero || TotalDelegated.RawValue.IsZero)
            {
                throw new InvalidOperationException("Cannot unbond without bonding.");
            }

            FungibleAssetValue fav = FAVToUnbond(share);
            delegation.CancelBond(fav, share);
            if (delegation.Bond.Share.IsZero)
            {
                Delegators = Delegators.Remove(delegator.Address);
            }

            TotalShares -= share;
            TotalDelegated -= fav;
            Distribute();
        }

        public void Distribute()
        {
            // TODO: Implement this
        }

        public Address BondAddress(Address delegatorAddress)
            => DeriveAddress(BondId, delegatorAddress);

        public Address UnbondLockInAddress(Address delegatorAddress)
            => DeriveAddress(UnbondLockInId, delegatorAddress);

        public Address RebondGraceAddress(Address delegatorAddress)
            => DeriveAddress(RebondGraceId, delegatorAddress);

        public override bool Equals(object obj)
            => obj is IDelegatee other && Equals(other);

        public bool Equals(IDelegatee other)
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
            && DelegateeId.SequenceEqual(delegatee.DelegateeId));

        public override int GetHashCode()
            => Address.GetHashCode();

        protected Address DeriveAddress(byte[] typeId, Address address)
            => DeriveAddress(typeId, address.ByteArray);

        protected Address DeriveAddress(byte[] typeId, IEnumerable<byte> bytes = null)
        {
            byte[] hashed;
            using (var hmac = new HMACSHA1(DelegateeId.Concat(typeId).ToArray()))
            {
                hashed = hmac.ComputeHash(
                    Address.ByteArray.Concat(bytes ?? Array.Empty<byte>()).ToArray());
            }

            return new Address(hashed);
        }
    }
}
