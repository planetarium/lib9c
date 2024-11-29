using System;
using Bencodex;
using Bencodex.Types;
using Nekoyume.TypedAddress;

namespace Nekoyume.Action.Guild.Migration.LegacyModels
{
    // TODO: [GuildMigration] Remove this class when the migration is done.
    /// <summary>
    /// The legacy model for GuildParticipant.
    /// </summary>
    public class LegacyGuildParticipant : IBencodable, IEquatable<LegacyGuildParticipant>
    {
        private const string StateTypeName = "guild_participant";
        private const long StateVersion = 1;

        public readonly GuildAddress GuildAddress;

        public LegacyGuildParticipant(GuildAddress guildAddress)
        {
            GuildAddress = guildAddress;
        }

        public LegacyGuildParticipant(List list) : this(new GuildAddress(list[2]))
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
            .Add(GuildAddress.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public bool Equals(LegacyGuildParticipant other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return GuildAddress.Equals(other.GuildAddress);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LegacyGuildParticipant)obj);
        }

        public override int GetHashCode()
        {
            return GuildAddress.GetHashCode();
        }
    }
}
