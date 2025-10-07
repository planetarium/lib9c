using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Lib9c.TypedAddress
{
    public readonly struct PledgeAddress : IBencodable
    {
        private readonly Address _address;

        public PledgeAddress(byte[] bytes) : this(new Address(bytes))
        {
        }

        public PledgeAddress(IValue value) : this(new Address(value))
        {
        }

        public PledgeAddress(string hex) : this(new Address(hex))
        {
        }

        public PledgeAddress(Address address)
        {
            _address = address;
        }

        public static implicit operator Address(PledgeAddress agentAddress)
        {
            return agentAddress._address;
        }

        public IValue Bencoded => _address.Bencoded;

        public static bool operator ==(PledgeAddress left, PledgeAddress right) => left.Equals(right);

        public static bool operator !=(PledgeAddress left, PledgeAddress right) => !left.Equals(right);

        public bool Equals(PledgeAddress other)
        {
            return _address.Equals(other._address);
        }

        public override bool Equals(object obj)
        {
            return obj is PledgeAddress other && Equals(other);
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
