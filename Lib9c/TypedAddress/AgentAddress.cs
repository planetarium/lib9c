using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Nekoyume.TypedAddress
{
    public readonly struct AgentAddress : IBencodable
    {
        private readonly Address _address;

        public AgentAddress(byte[] bytes) : this(new Address(bytes))
        {
        }

        public AgentAddress(IValue value) : this(new Address(value))
        {
        }

        public AgentAddress(string hex) : this(new Address(hex))
        {
        }

        public AgentAddress(Address address)
        {
            _address = address;
        }

        public static implicit operator Address(AgentAddress agentAddress)
        {
            return agentAddress._address;
        }

        public IValue Bencoded => _address.Bencoded;

        public static bool operator ==(AgentAddress left, AgentAddress right) => left.Equals(right);

        public static bool operator !=(AgentAddress left, AgentAddress right) => !left.Equals(right);

        public bool Equals(AgentAddress other)
        {
            return _address.Equals(other._address);
        }

        public override bool Equals(object obj)
        {
            return obj is AgentAddress other && Equals(other);
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
