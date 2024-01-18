using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Represents the state of a collection.
    /// </summary>
    public class CollectionState
    {
        public static Address Derive(Address avatarAddress) =>
            avatarAddress.Derive(nameof(CollectionState));

        public Address Address;
        public List<int> Ids = new();

        public CollectionState()
        {
        }

        public CollectionState(List serialized)
        {
            Address = serialized[0].ToAddress();
            var rawList = (List) serialized[1];
            foreach (var value in rawList)
            {
                Ids.Add((Integer)value);
            }
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Address.Serialize())
                .Add(new List(Ids));
        }
    }
}
