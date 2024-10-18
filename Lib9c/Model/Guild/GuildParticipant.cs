using System;
using Bencodex;
using Bencodex.Types;
using Nekoyume.Action;
using Nekoyume.Delegation;
using Nekoyume.TypedAddress;

namespace Nekoyume.Model.Guild
{
    public class GuildParticipant : Delegator<Guild, GuildParticipant>, IBencodable, IEquatable<GuildParticipant>
    {
        private const string StateTypeName = "guild_participant";
        private const long StateVersion = 1;

        public readonly GuildAddress GuildAddress;

        public GuildParticipant(
            AgentAddress address,
            GuildAddress guildAddress,
            GuildRepository repository)
            : base(
                  address: address,
                  accountAddress: Addresses.GuildParticipant,
                  delegationPoolAddress: address,
                  rewardAddress: address,
                  repository: repository)
        {
            GuildAddress = guildAddress;
        }

        public GuildParticipant(
            AgentAddress address,
            IValue bencoded,
            GuildRepository repository)
            : base(address: address, repository: repository)
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

            GuildAddress = new GuildAddress(list[2]);
        }

        public new AgentAddress Address => new AgentAddress(base.Address);

        public List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(GuildAddress.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public bool Equals(GuildParticipant other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Address.Equals(other.Address)
                 && GuildAddress.Equals(other.GuildAddress)
                 && Metadata.Equals(other.Metadata);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Guild)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address, GuildAddress);
        }
    }
}
