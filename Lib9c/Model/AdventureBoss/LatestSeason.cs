using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.AdventureBoss
{
    public class LatestSeason
    {
        public readonly long SeasonId;
        public readonly long StartBlockIndex;
        public readonly long EndBlockIndex;
        public readonly long NextStartBlockIndex;

        public LatestSeason()
        {
        }

        public LatestSeason(long season, long startBlockIndex, long endBlockIndex,
            long nextStartBlockIndex)
        {
            SeasonId = season;
            StartBlockIndex = startBlockIndex;
            EndBlockIndex = endBlockIndex;
            NextStartBlockIndex = nextStartBlockIndex;
        }

        public LatestSeason(IValue serialized)
        {
            SeasonId = ((List)serialized)[0].ToLong();
            StartBlockIndex = ((List)serialized)[1].ToLong();
            EndBlockIndex = ((List)serialized)[2].ToLong();
            NextStartBlockIndex = ((List)serialized)[3].ToLong();
        }

        public IValue Bencoded =>
            List.Empty
                .Add(SeasonId.Serialize())
                .Add(StartBlockIndex.Serialize())
                .Add(EndBlockIndex.Serialize())
                .Add(NextStartBlockIndex.Serialize());
    }
}
