using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;

namespace Nekoyume.Model.State
{
    public class ArenaState : IState
    {
        public static Address DeriveAddress(long startBlockIndex) =>
            Addresses.Arena.Derive($"arena_{startBlockIndex}");

        public Address Address;
        public readonly List<Address> AvatarAddresses;

        public ArenaState(long startBlockIndex)
        {
            Address = DeriveAddress(startBlockIndex);
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
                .Add(AvatarAddresses.Aggregate(List.Empty,
                    (list, address) => list.Add(address.Serialize())));
        }

        public void Add(Address address)
        {
            AvatarAddresses.Add(address);
        }
    }
}
