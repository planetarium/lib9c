using System.Collections.Generic;
using Bencodex;
using Bencodex.Types;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Represents the state of a collection.
    /// </summary>
    public class CollectionState : IBencodable
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

        public CollectionState(IValue bencoded) : this((List)bencoded)
        {
        }

        public IValue Bencoded => List.Empty.Add(new List(Ids));
    }
}
