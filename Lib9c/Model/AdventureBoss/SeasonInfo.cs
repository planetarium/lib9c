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
        public IEnumerable<Address> ParticipantList;
        public long UsedApPotion;
        public long UsedGoldenDust;
        public long UsedNcg;
        public long TotalPoint;

        public SeasonInfo()
        {
        }

        public SeasonInfo(long season, long blockIndex)
        {
            Season = season;
            StartBlockIndex = blockIndex;
            EndBlockIndex = StartBlockIndex + BossActiveBlockInterval;
            NextStartBlockIndex = EndBlockIndex + BossInactiveBlockInterval;
        }

        public SeasonInfo(IValue serialized)
        {
            Season = ((List)serialized)[0].ToInteger();
            StartBlockIndex = ((List)serialized)[1].ToInteger();
            EndBlockIndex = ((List)serialized)[2].ToInteger();
            NextStartBlockIndex = ((List)serialized)[3].ToInteger();
        }

        public IValue Bencoded =>
            List.Empty
                .Add(Season.Serialize())
                .Add(StartBlockIndex.Serialize())
                .Add(EndBlockIndex.Serialize())
                .Add(NextStartBlockIndex.Serialize());
    }
}
