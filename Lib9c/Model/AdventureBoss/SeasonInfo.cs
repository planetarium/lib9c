using Bencodex.Types;
using Lib9c.Model.State;

namespace Lib9c.Model.AdventureBoss
{
    public class SeasonInfo
    {
        public readonly long Season;
        public readonly long StartBlockIndex;
        public readonly long EndBlockIndex;
        public readonly long NextStartBlockIndex;

        public int BossId;

        public SeasonInfo(long season, long startBlockIndex, long activeInterval, long inactiveInterval, long? endBlockIndex = null, long? nextStartBlockIndex = null)
        {
            Season = season;
            StartBlockIndex = startBlockIndex;
            EndBlockIndex = endBlockIndex ?? StartBlockIndex + activeInterval;
            NextStartBlockIndex = nextStartBlockIndex ?? EndBlockIndex + inactiveInterval;
        }

        public SeasonInfo(List serialized)
        {
            Season = serialized[0].ToLong();
            StartBlockIndex = serialized[1].ToLong();
            EndBlockIndex = serialized[2].ToLong();
            NextStartBlockIndex = serialized[3].ToLong();
            BossId = serialized[4].ToInteger();
        }

        public IValue Bencoded =>
            List.Empty
                .Add(Season.Serialize())
                .Add(StartBlockIndex.Serialize())
                .Add(EndBlockIndex.Serialize())
                .Add(NextStartBlockIndex.Serialize())
                .Add(BossId.Serialize());
    }
}
