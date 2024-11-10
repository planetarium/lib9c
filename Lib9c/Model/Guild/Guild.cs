#nullable enable
using System;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.TypedAddress;

namespace Nekoyume.Model.Guild
{
    public class Guild : IBencodable, IEquatable<Guild>
    {
        private const string StateTypeName = "guild";
        private const long StateVersion = 1;

        public readonly AgentAddress GuildMasterAddress;

        public readonly Address ValidatorAddress;

        public Guild(
            GuildAddress address,
            AgentAddress guildMasterAddress,
            Address validatorAddress,
            GuildRepository repository)
        {
            Address = address;
            GuildMasterAddress = guildMasterAddress;
            ValidatorAddress = validatorAddress;
        }

        public Guild(
            GuildAddress address,
            IValue bencoded,
            GuildRepository repository)
        {
            if (bencoded is not List list)
            {
                throw new InvalidCastException();
            }

            if (list[0] is not Text text || text != StateTypeName || list[1] is not Integer integer)
            {
                throw new InvalidCastException();
            }

            if (integer > StateVersion)
            {
                throw new FailedLoadStateException("Un-deserializable state.");
            }

            Address = address;
            GuildMasterAddress = new AgentAddress(list[2]);
            ValidatorAddress = new AgentAddress(list[3]);
        }

        public GuildAddress Address { get; }

        public List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(GuildMasterAddress.Bencoded)
            .Add(ValidatorAddress.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public bool Equals(Guild? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Address.Equals(other.Address)
                 && GuildMasterAddress.Equals(other.GuildMasterAddress)
                 && ValidatorAddress.Equals(other.ValidatorAddress);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Guild)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address, GuildMasterAddress, ValidatorAddress);
        }
    }
}
