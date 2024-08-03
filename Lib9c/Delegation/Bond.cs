using System;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Nekoyume.Delegation
{
    public sealed class Bond : IBencodable, IEquatable<Bond>
    {
        public Bond(Address address)
            : this(address, BigInteger.Zero, 0)
        {
        }

        public Bond(Address address, IValue bencoded)
            : this(address, (List)bencoded)
        {
        }

        public Bond(Address address, List bencoded)
            : this(address, (Integer)bencoded[0], (Integer)bencoded[1])
        {
        }

        private Bond(Address address, BigInteger share, long lastDistributeHeight)
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

        public long LastDistributeHeight { get; }

        public IValue Bencoded => List.Empty
            .Add(Share)
            .Add(LastDistributeHeight);

        public override bool Equals(object obj)
            => obj is Bond other && Equals(other);

        public bool Equals(Bond other)
            => ReferenceEquals(this, other)
            || (Address.Equals(other.Address)
            && Share.Equals(other.Share)
            && LastDistributeHeight.Equals(other.LastDistributeHeight));

        public override int GetHashCode()
            => Address.GetHashCode();

        internal Bond AddShare(BigInteger share)
        {
            if (share.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(share),
                    share,
                    "share must be positive.");
            }

            return new Bond(Address, Share + share, LastDistributeHeight);
        }

        internal Bond SubtractShare(BigInteger share)
        {
            if (share > Share)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(share),
                    share,
                    "share must be less than or equal to the current share.");
            }

            return new Bond(Address, Share - share, LastDistributeHeight);
        }

        internal Bond UpdateLastDistributeHeight(long height)
        {
            if (height <= LastDistributeHeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    "height must be greater than the last distribute height.");
            }

            return new Bond(Address, Share, height);
        }
    }
}
