using System;
using Bencodex;
using Bencodex.Types;

namespace Nekoyume.TypedAddress
{
    using Address = Libplanet.Crypto.Address;

    public readonly struct GuildAddress : IBencodable, IEquatable<GuildAddress>
    {
        private readonly Address _address;

        public GuildAddress(byte[] bytes) : this(new Address(bytes))
        {
        }

        public GuildAddress(string hex) : this(new Address(hex))
        {
        }

        public GuildAddress(IValue value) : this(new Address(value))
        {
        }

        public GuildAddress(Address address)
        {
            _address = address;
        }

        public static implicit operator Address(GuildAddress guildAddress)
        {
            return guildAddress._address;
        }

        public IValue Bencoded => _address.Bencoded;

        public static bool operator ==(GuildAddress left, GuildAddress right) => left.Equals(right);

        public static bool operator !=(GuildAddress left, GuildAddress right) => !left.Equals(right);

        public bool Equals(GuildAddress other)
        {
            return _address.Equals(other._address);
        }

        public override bool Equals(object obj)
        {
            return obj is GuildAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _address.GetHashCode();
        }

        public override string ToString()
        {
            return _address.ToString();
        }
    }
}
