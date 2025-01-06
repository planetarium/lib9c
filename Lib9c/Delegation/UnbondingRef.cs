#nullable enable
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using System;

namespace Nekoyume.Delegation
{
    public readonly struct UnbondingRef : IComparable, IBencodable
    {
        public UnbondingRef(Address address, UnbondingType unbondingType)
        {
            Address = address;
            UnbondingType = unbondingType;
        }

        public UnbondingRef(IValue bencoded)
            : this((List)bencoded)
        {
        }

        public UnbondingRef(List list)
            : this(new Address((Binary)list[0]), (UnbondingType)(int)(Integer)list[1])
        {
        }

        public Address Address { get; }

        public UnbondingType UnbondingType { get; }

        public List Bencoded => List.Empty
            .Add(Address.Bencoded)
            .Add((int)UnbondingType);

        public static bool operator ==(UnbondingRef left, UnbondingRef right)
            => left.Equals(right);

        public static bool operator !=(UnbondingRef left, UnbondingRef right)
            => !(left == right);

        public static bool operator <(UnbondingRef left, UnbondingRef right)
            => left.CompareTo(right) < 0;

        public static bool operator <=(UnbondingRef left, UnbondingRef right)
            => left.CompareTo(right) <= 0;

        public static bool operator >(UnbondingRef left, UnbondingRef right)
            => left.CompareTo(right) > 0;

        public static bool operator >=(UnbondingRef left, UnbondingRef right)
            => left.CompareTo(right) >= 0;

        IValue IBencodable.Bencoded => Bencoded;

        public int CompareTo(object? obj)
            => obj is UnbondingRef other
                ? CompareTo(other)
                : throw new ArgumentException("Object is not a UnbondingRef.");

        public int CompareTo(UnbondingRef? other)
        {
            if (other is not { } otherRef)
            {
                return 1;
            }

            int addressComparison = Address.CompareTo(otherRef.Address);
            if (addressComparison != 0)
            {
                return addressComparison;
            }

            return UnbondingType.CompareTo(otherRef.UnbondingType);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not UnbondingRef other)
            {
                return false;
            }

            return Address == other.Address && UnbondingType == other.UnbondingType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address, UnbondingType);
        }
    }
}
