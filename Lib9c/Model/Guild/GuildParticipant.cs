using System;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
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
            AgentAddress agentAddress, GuildAddress guildAddress)
            : this(agentAddress, guildAddress, null)
        {
        }

        public GuildParticipant(AgentAddress agentAddress, List list)
            : this(agentAddress, list, null)
        {
        }

        public GuildParticipant(
            AgentAddress agentAddress, GuildAddress guildAddress, IDelegationRepository repository)
            : base(agentAddress, repository)
        {
            GuildAddress = guildAddress;
        }

        public GuildParticipant(AgentAddress agentAddress, List list, IDelegationRepository repository)
            : base(agentAddress, list[3], repository)
        {
            GuildAddress = new GuildAddress(list[2]);

            if (list[0] is not Text text || text != StateTypeName || list[1] is not Integer integer)
            {
                throw new InvalidCastException();
            }

            if (integer > StateVersion)
            {
                throw new FailedLoadStateException("Un-deserializable state.");
            }
        }

        public AgentAddress AgentAddress => new AgentAddress(Address);

        public new List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(GuildAddress.Bencoded)
            .Add(base.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public bool Equals(GuildParticipant other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return GuildAddress.Equals(other.GuildAddress)
                && base.Equals(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GuildParticipant)obj);
        }

        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }
    }
}
