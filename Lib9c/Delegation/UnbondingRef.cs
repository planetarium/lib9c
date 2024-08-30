#nullable enable
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using System;

namespace Nekoyume.Delegation
{
    public class UnbondingRef : IEquatable<UnbondingRef>, IComparable<UnbondingRef>, IComparable, IBencodable
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

        public static bool operator ==(UnbondingRef? left, UnbondingRef? right)
            => left?.Equals(right) ?? right is null;

        public static bool operator !=(UnbondingRef? left, UnbondingRef? right)
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
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (other is null)
            {
                return 1;
            }

            int addressComparison = Address.CompareTo(other.Address);
            if (addressComparison != 0)
            {
                return addressComparison;
            }

            return UnbondingType.CompareTo(other.UnbondingType);
        }

        public override bool Equals(object? obj)
            => obj is UnbondingRef other && Equals(other);

        public bool Equals(UnbondingRef? other)
            => ReferenceEquals(this, other)
            || (other is UnbondingRef unbondingRef
            && Address.Equals(unbondingRef.Address)
            && UnbondingType == unbondingRef.UnbondingType);

        public override int GetHashCode()
        {
            return HashCode.Combine(Address, UnbondingType);
        }
    }
}
