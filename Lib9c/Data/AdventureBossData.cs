using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet.Types.Assets;

namespace Nekoyume.Data
{
    public static class AdventureBossData
    {
        public struct RewardInfo
        {
            // Dictionary of (id/ticker, ratio) pairs.
            public Dictionary<int, int> FixedRewardItemIdDict;
            public Dictionary<int, int> FixedRewardFavIdDict;
            public Dictionary<int, int> RandomRewardItemIdDict;
            public Dictionary<int, int> RandomRewardFavTickerDict;
        }

        public struct ExploreReward
        {
            public string RewardType;
            public int RewardId;
            public int Amount;
            public int Ratio;
        }

        public struct ExploreRewardData
        {
            public ExploreReward[] FirstReward;
            public ExploreReward[] Reward;
        }

        public struct AdventureBossReward
        {
            public int BossId;
            public RewardInfo wantedReward;
            public RewardInfo contributionReward;
            public ImmutableDictionary<int, ExploreRewardData> exploreReward;
        }

        public static readonly ImmutableDictionary<int, decimal> NcgRewardRatio =
            new Dictionary<int, decimal>
            {
                { 600201, 0.5m }, // Golden Dust : 0.5
                { 600202, 1.5m }, // Ruby Dust : 1.5
                { 600203, 7.5m }, // Diamond Dust : 7.5
                { 600301, 0.1m }, // Normal Hammer : 0.1
                { 600302, 0.5m }, // Rare Hammer : 0.5
                { 600303, 1m }, // Epic Hammer : 1
                { 600304, 4m }, // Unique Hammer : 4
                { 600305, 15m }, // Legendary Hammer : 15
                // { 600306, 60m }, // Divinity Hammer : 60
            }.ToImmutableDictionary();

        public const decimal NcgRuneRatio = 2.5m;

        // FIXME: This may temporary
        public static readonly ImmutableArray<AdventureBossReward> WantedRewardList =
            new AdventureBossReward[]
            {
                new ()
                {
                    BossId = 206007,
                    wantedReward = new RewardInfo
                    {
                        FixedRewardItemIdDict = new Dictionary<int, int>
                        {
                            { 600201, 100 }
                        },
                        FixedRewardFavIdDict = new Dictionary<int, int>(),
                        RandomRewardItemIdDict = new Dictionary<int, int>
                        {
                            { 600201, 20 }, { 600202, 20 }, { 600203, 20 }
                        },
                        RandomRewardFavTickerDict = new Dictionary<int, int>
                        {
                            { 20001, 20 }, { 30001, 20 }
                        }
                    },
                    contributionReward = new RewardInfo
                    {
                        FixedRewardItemIdDict = new Dictionary<int, int>
                        {
                            { 600202, 100 }
                        },
                        FixedRewardFavIdDict = new Dictionary<int, int>(),
                        RandomRewardItemIdDict = new Dictionary<int, int>(),
                        RandomRewardFavTickerDict = new Dictionary<int, int>(),
                    },
                    exploreData = new Dictionary<int, ExploreData>
                    {
                        {
                            1, new ExploreData
                            {
                            }
                        }
                    }.ToImmutableDictionary()
                },
                new ()
                {
                    BossId = 208007,
                    wantedReward = new RewardInfo
                    {
                        FixedRewardItemIdDict = new Dictionary<int, int>
                        {
                            { 600202, 100 }
                        },
                        FixedRewardFavIdDict = new Dictionary<int, int>(),
                        RandomRewardItemIdDict = new Dictionary<int, int>
                        {
                            { 600201, 20 }, { 600202, 20 }, { 600203, 20 }
                        },
                        RandomRewardFavTickerDict = new Dictionary<int, int>
                        {
                            { 20001, 20 }, { 30001, 20 }
                        }
                    },
                    contributionReward = new RewardInfo
                    {
                        FixedRewardItemIdDict = new Dictionary<int, int>
                        {
                            { 600202, 100 }
                        },
                        FixedRewardFavIdDict = new Dictionary<int, int>(),
                        RandomRewardItemIdDict = new Dictionary<int, int>(),
                        RandomRewardFavTickerDict = new Dictionary<int, int>(),
                    }
                },
                new ()
                {
                    BossId = 207007,
                    wantedReward = new RewardInfo
                    {
                        FixedRewardItemIdDict = new Dictionary<int, int>(),
                        FixedRewardFavIdDict = new Dictionary<int, int>
                        {
                            { 20001, 50 }, { 30001, 50 }
                        },
                        RandomRewardItemIdDict = new Dictionary<int, int>
                        {
                            { 600201, 20 }, { 600202, 20 }, { 600203, 20 }
                        },
                        RandomRewardFavTickerDict = new Dictionary<int, int>
                        {
                            { 20001, 20 }, { 30001, 20 }
                        }
                    },
                    contributionReward = new RewardInfo
                    {
                        FixedRewardItemIdDict = new Dictionary<int, int>
                        {
                            { 600203, 100 }
                        },
                        FixedRewardFavIdDict = new Dictionary<int, int>(),
                        RandomRewardItemIdDict = new Dictionary<int, int>(),
                        RandomRewardFavTickerDict = new Dictionary<int, int>(),
                    }
                },
                new ()
                {
                    BossId = 209007,
                    wantedReward = new RewardInfo
                    {
                        FixedRewardItemIdDict = new Dictionary<int, int>
                        {
                            { 600203, 100 }
                        },
                        FixedRewardFavIdDict = new Dictionary<int, int>(),
                        RandomRewardItemIdDict = new Dictionary<int, int>
                        {
                            { 600201, 20 }, { 600202, 20 }, { 600203, 20 }
                        },
                        RandomRewardFavTickerDict = new Dictionary<int, int>
                        {
                            { 20001, 20 }, { 30001, 20 }
                        }
                    },
                    contributionReward = new RewardInfo
                    {
                        FixedRewardItemIdDict = new Dictionary<int, int>
                        {
                            { 600203, 100 }
                        },
                        FixedRewardFavIdDict = new Dictionary<int, int>(),
                        RandomRewardItemIdDict = new Dictionary<int, int>(),
                        RandomRewardFavTickerDict = new Dictionary<int, int>(),
                    }
                },
            }.ToImmutableArray();

        public static readonly ImmutableDictionary<int, (int, int)> PointDict =
            new Dictionary<int, (int, int)>
            {
                { 1, (100, 300) },
                { 2, (270, 490) },
                { 3, (440, 680) },
                { 4, (610, 870) },
                { 5, (780, 1060) },
                { 6, (950, 1250) },
                { 7, (1120, 1440) },
                { 8, (1290, 1630) },
                { 9, (1460, 1820) },
                { 10, (1630, 2010) },
                { 11, (1800, 2200) },
                { 12, (1870, 2390) },
                { 13, (2140, 2580) },
                { 14, (2310, 2770) },
                { 15, (2480, 2960) },
                { 16, (2650, 3150) },
                { 17, (2820, 3340) },
                { 18, (2990, 3530) },
                { 19, (3160, 3720) },
                { 20, (3330, 3910) },
            }.ToImmutableDictionary();

        public struct ClaimableReward
        {
            public FungibleAssetValue? NcgReward;
            public Dictionary<int, int> ItemReward;
            public Dictionary<int, int> FavReward;
        }
    }
}
