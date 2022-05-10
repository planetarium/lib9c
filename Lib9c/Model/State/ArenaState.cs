using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;

namespace Nekoyume.Model.State
{
    public class ArenaState : IState
    {
        public static Address DeriveAddress(long index) => Addresses.Arena.Derive($"arena_{index}");

        public Address Address;
        public readonly List<Address> AvatarAddresses;

        public ArenaState(long index)
        {
            Address = DeriveAddress(index);
            AvatarAddresses = new List<Address>();
        }

        public ArenaState(List serialized)
        {
            Address = serialized[0].ToAddress();
            AvatarAddresses = ((List)serialized[1]).Select(c => c.ToAddress()).ToList();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Address.Serialize())
                .Add(AvatarAddresses.Aggregate(List.Empty, (list, address) => list.Add(address.Serialize())));
        }

        public void Add(Address address)
        {
            AvatarAddresses.Add(address);
        }
    }
}
