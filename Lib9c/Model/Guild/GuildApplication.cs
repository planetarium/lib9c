using System;
using Bencodex;
using Bencodex.Types;
using Nekoyume.Action;
using Nekoyume.TypedAddress;

namespace Nekoyume.Model.Guild
{
    public class GuildApplication : IBencodable
    {
        private const string StateTypeName = "guild_application";
        private const long StateVersion = 1;

        public readonly GuildAddress GuildAddress;

        public GuildApplication(GuildAddress guildAddress)
        {
            GuildAddress = guildAddress;
        }

        public GuildApplication(List list) : this(new GuildAddress(list[2]))
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

        public List Bencoded => new(
            (Text)StateTypeName,
            (Integer)StateVersion,
            GuildAddress.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;
    }
}
