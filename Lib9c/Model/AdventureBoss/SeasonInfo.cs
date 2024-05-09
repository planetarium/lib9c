using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Action.AdventureBoss;
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
        public int? FixedRewardItemId = null;
        public int? FixedRewardFavTicker = null;
        public int? RandomRewardItemId = null;
        public int? RandomRewardFavTicker = null;
        public double RaffleRewardRatio;

        public HashSet<Address> ExplorerList;
        public long UsedApPotion;
        public long UsedGoldenDust;
        public long UsedNcg;
        public long TotalPoint;

        public SeasonInfo(long season, long blockIndex, IEnumerable<Address> participantList = null)
        {
            Season = season;
            StartBlockIndex = blockIndex;
            EndBlockIndex = StartBlockIndex + BossActiveBlockInterval;
            NextStartBlockIndex = EndBlockIndex + BossInactiveBlockInterval;
            ExplorerList = participantList is null
                ? new HashSet<Address>()
                : participantList.ToHashSet();
        }

        public SeasonInfo(List serialized)
        {
            Season = serialized[0].ToInteger();
            StartBlockIndex = serialized[1].ToInteger();
            EndBlockIndex = serialized[2].ToInteger();
            NextStartBlockIndex = serialized[3].ToInteger();
            ExplorerList = ((List)serialized[4]).Select(e => e.ToAddress()).ToHashSet();
        }

        public void SetSeasonData(WantedReward[] wantedRewardList, IRandom random)
        {
            var wantedReward = wantedRewardList[random.Next(0, wantedRewardList.Length)];
            BossId = wantedReward.BossId;
            if (wantedReward.FixedRewardItemIdList.Length > 0)
            {
                FixedRewardItemId =
                    wantedReward.FixedRewardItemIdList[
                        random.Next(0, wantedReward.FixedRewardItemIdList.Length)
                    ];
            }
            if (wantedReward.FixedRewardFavTickerList.Length > 0)
            {
                FixedRewardFavTicker =
                    wantedReward.FixedRewardFavTickerList[
                        random.Next(0, wantedReward.FixedRewardFavTickerList.Length)
                    ];
            }
            if (wantedReward.RandomRewardItemIdList.Length > 0)
            {
                RandomRewardItemId =
                    wantedReward.RandomRewardItemIdList[
                        random.Next(0, wantedReward.RandomRewardItemIdList.Length)
                    ];
            }
            if (wantedReward.RandomRewardFavTickerList.Length > 0)
            {
                RandomRewardFavTicker =
                    wantedReward.RandomRewardFavTickerList[
                        random.Next(0, wantedReward.RandomRewardFavTickerList.Length)
                    ];
            }

            RaffleRewardRatio = wantedReward.RaffleRewardRatio;
        }

        public void AddExplorer(Address avatarAddress)
        {
            ExplorerList.Add(avatarAddress);
        }

        public IValue Bencoded =>
            List.Empty
                .Add(Season.Serialize())
                .Add(StartBlockIndex.Serialize())
                .Add(EndBlockIndex.Serialize())
                .Add(NextStartBlockIndex.Serialize())
                .Add(new List(ExplorerList.OrderBy(x => x).Select(x => x.Serialize())));
    }
}
