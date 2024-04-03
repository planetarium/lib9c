using System.Collections.Generic;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

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

            Ids = Ids.Distinct().ToList();
        }

        public CollectionState(IValue bencoded) : this((List)bencoded)
        {
        }

        public IValue Bencoded => List.Empty.Add(new List(Ids.Distinct()));

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
