using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.State;
using Libplanet.Crypto;

namespace Lib9c.Model.AdventureBoss
{
    public class ExplorerList
    {
        public long Season;
        public HashSet<(Address, string)> Explorers = new ();

        public ExplorerList(long season)
        {
            Season = season;
        }

        public ExplorerList(List bencoded)
        {
            Season = (Integer)bencoded[0];
            Explorers = bencoded[1].ToHashSet(
                i => (((List)i)[0].ToAddress(), ((List)i)[1].ToDotnetString())
            );
        }

        public void AddExplorer(Address avatarAddress, string name)
        {
            Explorers.Add((avatarAddress, name));
        }

        public IValue Bencoded => List.Empty.Add(Season).Add(new List(Explorers.OrderBy(e => e)
            .Select(e => new List(e.Item1.Serialize(), (Text)e.Item2)))
        );
    }
}
