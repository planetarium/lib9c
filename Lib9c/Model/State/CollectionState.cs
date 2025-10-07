using System.Collections.Generic;
using Bencodex;
using Bencodex.Types;
using Lib9c.Model.Stat;
using Lib9c.TableData;

namespace Lib9c.Model.State
{
    /// <summary>
    /// Represents the state of a collection.
    /// </summary>
    public class CollectionState : IBencodable
    {
        public SortedSet<int> Ids = new();

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

        public List<StatModifier> GetModifiers(CollectionSheet collectionSheet)
        {
            var collectionModifiers = new List<StatModifier>();
            foreach (var collectionId in Ids)
            {
                collectionModifiers.AddRange(collectionSheet[collectionId].StatModifiers);
            }

            return collectionModifiers;
        }
    }
}
