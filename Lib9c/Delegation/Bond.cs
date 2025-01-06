#nullable enable
using System;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Nekoyume.Delegation
{
    public readonly struct Bond : IBencodable
    {
        public Bond(Address address)
            : this(address, BigInteger.Zero, null)
        {
        }

        public Bond(Address address, IValue bencoded)
            : this(address, (List)bencoded)
        {
        }

        public Bond(Address address, List bencoded)
            : this(
                  address,
                  (Integer)bencoded[0],
                  bencoded[1] is Integer share ? share : null)
        {
        }

        private Bond(Address address, BigInteger share, long? lastDistributeHeight)
        {
            if (share.Sign < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(share),
                    share,
                    "Share must be non-negative.");
            }

            if (lastDistributeHeight < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lastDistributeHeight),
                    lastDistributeHeight,
                    "Last distribute height must be non-negative.");
            }

            Address = address;
            Share = share;
            LastDistributeHeight = lastDistributeHeight;
        }

        public Address Address { get; }

        public BigInteger Share { get; }

        public long? LastDistributeHeight { get; }

        public bool IsEmpty => Share.IsZero && LastDistributeHeight is null;

        public List Bencoded => List.Empty
            .Add(Share)
            .Add(LastDistributeHeight.HasValue
                ? new Integer(LastDistributeHeight.Value)
                : Null.Value);

        IValue IBencodable.Bencoded => Bencoded;

        public static bool operator ==(Bond left, Bond right)
            => left.Equals(right);

        public static bool operator !=(Bond left, Bond right)
            => !left.Equals(right);

        public override bool Equals(object? obj)
        {
            if (obj is not Bond other)
            {
                return false;
            }

            return Address == other.Address
                && Share == other.Share
                && LastDistributeHeight == other.LastDistributeHeight;
        }

        public override int GetHashCode()
            => HashCode.Combine(Address, Share, LastDistributeHeight);

        internal Bond AddShare(BigInteger value)
        {
            if (value.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "share must be positive.");
            }

            var share = Share + value;
            var lastDistributeHeight = LastDistributeHeight;
            return new Bond(Address, share, lastDistributeHeight);
        }

        internal Bond SubtractShare(BigInteger value)
        {
            if (value > Share)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "share must be less than or equal to the current share.");
            }

            var share = Share - value;
            var lastDistributeHeight = share.IsZero ? null : LastDistributeHeight;
            return new Bond(Address, share, lastDistributeHeight);
        }

        internal Bond UpdateLastDistributeHeight(long height)
        {
            // TODO: [GuildMigration] Revive below check after migration
            // if (LastDistributeHeight.HasValue && LastDistributeHeight >= height)
            // {
            //     throw new ArgumentOutOfRangeException(
            //         nameof(height),
            //         height,
            //         "height must be greater than the last distribute height.");
            // }

            return new Bond(Address, Share, height);
        }

        internal Bond ClearLastDistributeHeight()
        {
            return new Bond(Address, Share, null);
        }
    }
}
