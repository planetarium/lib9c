using System;
using Bencodex;
using Bencodex.Types;
using Nekoyume.TypedAddress;

namespace Nekoyume.Action.Guild.Migration.LegacyModels
{
    // TODO: [GuildMigration] Remove this class when the migration is done.
    public class LegacyGuild : IEquatable<LegacyGuild>, IBencodable
    {
        private const string StateTypeName = "guild";
        private const long StateVersion = 1;

        public readonly AgentAddress GuildMasterAddress;

        public LegacyGuild(AgentAddress guildMasterAddress)
        {
            GuildMasterAddress = guildMasterAddress;
        }

        public LegacyGuild(List list) : this(new AgentAddress(list[2]))
        {
            if (list[0] is not Text text || text != StateTypeName || list[1] is not Integer integer)
            {
                throw new InvalidCastException();
            }

            if (integer > StateVersion)
            {
                throw new FailedLoadStateException("Un-deserializable state.");
            }
        }

        public List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(GuildMasterAddress.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public bool Equals(LegacyGuild other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return GuildMasterAddress.Equals(other.GuildMasterAddress);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((LegacyGuild)obj);
        }

        public override int GetHashCode()
        {
            return GuildMasterAddress.GetHashCode();
        }
    }
}
