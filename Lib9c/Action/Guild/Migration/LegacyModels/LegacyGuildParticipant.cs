using System;
using Bencodex;
using Bencodex.Types;
using Lib9c.TypedAddress;

namespace Lib9c.Action.Guild.Migration.LegacyModels
{
    // TODO: [GuildMigration] Remove this class when the migration is done.
    /// <summary>
    /// The legacy model for GuildParticipant.
    /// </summary>
    public class LegacyGuildParticipant : IBencodable, IEquatable<LegacyGuildParticipant>
    {
        /// <summary>
        /// The type name of the state.
        /// </summary>
        private const string StateTypeName = "guild_participant";

        /// <summary>
        /// The version of the state.
        /// </summary>
        private const long StateVersion = 1;

        /// <summary>
        /// The guild address.
        /// </summary>
        public readonly GuildAddress GuildAddress;

        /// <summary>
        /// Constructor of LegacyGuildParticipant.
        /// </summary>
        /// <param name="guildAddress">
        /// The guild address.
        /// </param>
        public LegacyGuildParticipant(GuildAddress guildAddress)
        {
            GuildAddress = guildAddress;
        }

        /// <summary>
        /// Constructor of LegacyGuildParticipant.
        /// </summary>
        /// <param name="list">
        /// The serialized data.
        /// </param>
        /// <exception cref="InvalidCastException">
        /// Throws when the deserialization failed.
        /// </exception>
        /// <exception cref="FailedLoadStateException">
        /// Throws when the state is un-deserializable.
        /// </exception>
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

        /// <summary>
        /// Serialize the state.
        /// </summary>
        public List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(GuildAddress.Bencoded);

        /// <inheritdoc/>
        IValue IBencodable.Bencoded => Bencoded;

        /// <inheritdoc/>
        public bool Equals(LegacyGuildParticipant other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return GuildAddress.Equals(other.GuildAddress);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LegacyGuildParticipant)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return GuildAddress.GetHashCode();
        }
    }
}
