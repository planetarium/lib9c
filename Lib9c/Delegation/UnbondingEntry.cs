#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Delegation
{
    public class UnbondingEntry : IBencodable, IEquatable<UnbondingEntry>
    {
        private int? _cachedHashCode;

        public UnbondingEntry(
            Address unbondeeAddress,
            FungibleAssetValue unbondingFAV,
            long creationHeight,
            long expireHeight)
            : this(unbondeeAddress, unbondingFAV, unbondingFAV, creationHeight, expireHeight)
        {
        }

        public UnbondingEntry(IValue bencoded)
            : this((List)bencoded)
        {
        }

        private UnbondingEntry(List bencoded)
            : this(
                  new Address(bencoded[0]),
                  new FungibleAssetValue(bencoded[1]),
                  new FungibleAssetValue(bencoded[2]),
                  (Integer)bencoded[3],
                  (Integer)bencoded[4])
        {
        }

        public UnbondingEntry(
                Address unbondeeAddress,
                FungibleAssetValue initialUnbondingFAV,
                FungibleAssetValue unbondingFAV,
                long creationHeight,
                long expireHeight)
        {
            if (initialUnbondingFAV.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialUnbondingFAV),
                    initialUnbondingFAV,
                    "The initial unbonding FAV must be greater than zero.");
            }

            if (unbondingFAV.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unbondingFAV),
                    unbondingFAV,
                    "The unbonding FAV must be greater than zero.");
            }

            if (unbondingFAV > initialUnbondingFAV)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unbondingFAV),
                    unbondingFAV,
                    "The unbonding FAV must be less than or equal to the initial unbonding FAV.");
            }

            if (creationHeight < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(creationHeight),
                    creationHeight,
                    "The creation height must be greater than or equal to zero.");
            }

            if (expireHeight < creationHeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expireHeight),
                    expireHeight,
                    "The expire height must be greater than the creation height.");
            }

            UnbondeeAddress = unbondeeAddress;
            InitialUnbondingFAV = initialUnbondingFAV;
            UnbondingFAV = unbondingFAV;
            CreationHeight = creationHeight;
            ExpireHeight = expireHeight;
        }

        public Address UnbondeeAddress { get; }

        public FungibleAssetValue InitialUnbondingFAV { get; }

        public FungibleAssetValue UnbondingFAV { get; }

        public long CreationHeight { get; }

        public long ExpireHeight { get; }

        public List Bencoded => List.Empty
            .Add(UnbondeeAddress.Bencoded)
            .Add(InitialUnbondingFAV.Serialize())
            .Add(UnbondingFAV.Serialize())
            .Add(CreationHeight)
            .Add(ExpireHeight);

        IValue IBencodable.Bencoded => Bencoded;

        public override bool Equals(object? obj)
            => obj is UnbondingEntry other && Equals(other);

        public bool Equals(UnbondingEntry? other)
            => ReferenceEquals(this, other)
            || (other is UnbondingEntry rebondGraceEntry
            && UnbondeeAddress.Equals(rebondGraceEntry.UnbondeeAddress)
            && InitialUnbondingFAV.Equals(rebondGraceEntry.InitialUnbondingFAV)
            && UnbondingFAV.Equals(rebondGraceEntry.UnbondingFAV)
            && CreationHeight == rebondGraceEntry.CreationHeight
            && ExpireHeight == rebondGraceEntry.ExpireHeight);

        public override int GetHashCode()
        {
            if (_cachedHashCode is int cached)
            {
                return cached;
            }

            int hash = HashCode.Combine(
                UnbondeeAddress,
                InitialUnbondingFAV,
                UnbondingFAV,
                CreationHeight,
                ExpireHeight);

            _cachedHashCode = hash;
            return hash;
        }

        public UnbondingEntry Slash(
            BigInteger slashFactor, long infractionHeight, out FungibleAssetValue slashedFAV)
        {
            if (CreationHeight > infractionHeight ||
                ExpireHeight < infractionHeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(infractionHeight),
                    infractionHeight,
                    "The infraction height must be between in creation height and expire height of entry.");
            }

            var favToSlash = InitialUnbondingFAV.DivRem(slashFactor, out var rem);
            if (rem.Sign > 0)
            {
                favToSlash += FungibleAssetValue.FromRawValue(rem.Currency, 1);
            }

            slashedFAV = favToSlash < UnbondingFAV
                ? favToSlash
                : UnbondingFAV;

            return new UnbondingEntry(
                UnbondeeAddress,
                InitialUnbondingFAV,
                UnbondingFAV - slashedFAV,
                CreationHeight,
                ExpireHeight);
        }

        internal UnbondingEntry Cancel(FungibleAssetValue cancellingFAV)
        {
            if (cancellingFAV.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cancellingFAV),
                    cancellingFAV,
                    "The cancelling FAV must be greater than zero.");
            }

            if (UnbondingFAV <= cancellingFAV)
            {
                throw new InvalidOperationException("Cannot cancel more than unbonding FAV.");
            }

            return new UnbondingEntry(
                UnbondeeAddress,
                InitialUnbondingFAV - cancellingFAV,
                UnbondingFAV - cancellingFAV,
                CreationHeight,
                ExpireHeight);
        }

        public class Comparer : IComparer<UnbondingEntry>
        {
            public int Compare(UnbondingEntry? x, UnbondingEntry? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x is null)
                {
                    return -1;
                }

                if (y is null)
                {
                    return 1;
                }

                int comparison = x.ExpireHeight.CompareTo(y.ExpireHeight);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = x.CreationHeight.CompareTo(y.CreationHeight);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = -x.InitialUnbondingFAV.CompareTo(y.InitialUnbondingFAV);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = -x.UnbondingFAV.CompareTo(y.UnbondingFAV);
                if (comparison != 0)
                {
                    return comparison;
                }

                return x.UnbondeeAddress.CompareTo(y.UnbondeeAddress);
            }
        }
    }
}
