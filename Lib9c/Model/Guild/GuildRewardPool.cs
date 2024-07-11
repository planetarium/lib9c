using System;
using System.Collections.Immutable;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;

namespace Nekoyume.Model.Guild
{
    public class GuildRewardPool : IBencodable
    {
        private const string StateTypeName = "guild_reward_pool";
        private const long StateVersion = 1;

        private readonly ImmutableHashSet<Address> _addresses;

        public IOrderedEnumerable<Address> Addresses => _addresses.OrderBy(x => x);

        public GuildRewardPool(ImmutableHashSet<Address> addresses)
        {
            _addresses = addresses;
        }

        public GuildRewardPool(List list)
            : this(((List)list[2]).Select(x => new Address(x)).ToImmutableHashSet())
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

        public GuildRewardPool Add(Address address)
        {
            return new GuildRewardPool(_addresses.Add(address));
        }

        public GuildRewardPool Remove(Address address)
        {
            return new GuildRewardPool(_addresses.Remove(address));
        }

        public List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(new List(_addresses.OrderBy(x => x).Select(x => x.Bencoded)));

        IValue IBencodable.Bencoded => Bencoded;
    }
}
