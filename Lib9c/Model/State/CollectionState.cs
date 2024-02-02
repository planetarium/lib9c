using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Represents the state of a collection.
    /// </summary>
    public class CollectionState
    {
        public List<int> Ids = new();

        public CollectionState()
        {
        }

        public CollectionState(List serialized)
        {
            var rawList = (List) serialized[0];
            foreach (var value in rawList)
            {
                Ids.Add((Integer)value);
            }
        }

        public IValue SerializeList()
        {
            return List.Empty
                .Add(new List(Ids));
        }
    }
}
