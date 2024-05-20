using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Model.State;

namespace Nekoyume.Model.AdventureBoss
{
    public class SeasonInfo
    {
        // FIXME: Interval must be changed before release
        public const long BossActiveBlockInterval = 10_000L;
        public const long BossInactiveBlockInterval = 10_000L;

        public readonly long Season;
        public readonly long StartBlockIndex;
        public readonly long EndBlockIndex;
        public readonly long NextStartBlockIndex;

        public int BossId;

        public SeasonInfo(long season, long blockIndex, IEnumerable<Address> participantList = null)
        {
            Season = season;
            StartBlockIndex = blockIndex;
            EndBlockIndex = StartBlockIndex + BossActiveBlockInterval;
            NextStartBlockIndex = EndBlockIndex + BossInactiveBlockInterval;
        }

        public SeasonInfo(List serialized)
        {
            Season = serialized[0].ToInteger();
            StartBlockIndex = serialized[1].ToInteger();
            EndBlockIndex = serialized[2].ToInteger();
            NextStartBlockIndex = serialized[3].ToInteger();
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
