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
                    exploreReward = new Dictionary<int, ExploreRewardData>
                    {
                        {
                            1,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 10,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 10,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 10,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 10,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 10,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            2,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 20,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 20,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 20,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 20,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 20,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            3,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 30,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 30,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 30,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 30,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 30,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            4,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 40,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 40,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 40,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 40,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 40,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            5,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 50,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 50,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 50,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 50,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 50,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            6,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 60,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 60,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 60,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 60,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 60,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            7,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 70,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 70,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 70,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 70,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 70,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            8,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 80,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 80,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 80,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 80,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 80,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            9,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 90,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 90,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 90,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 90,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 90,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            10,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 100,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 100,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 100,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 100,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 100,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            11,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 110,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 110,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 110,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 110,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 110,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            12,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 120,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 120,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 120,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 120,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 120,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            13,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 130,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 130,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 130,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 130,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 130,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            14,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 140,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 140,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 140,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 140,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 140,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            15,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 150,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 150,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 150,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 150,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 150,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            16,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 160,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 160,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 160,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 160,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 160,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            17,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 170,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 170,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 170,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 170,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 170,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            18,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 180,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 180,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 180,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 180,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 180,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            19,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 190,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 190,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 190,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 190,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 190,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                        {
                            20,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 200,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 200,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 200,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 200,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 200,
                                        Ratio = 20,
                                    },
                                },
                            }
                        },
                    }.ToImmutableDictionary(),
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
                    },
                    exploreReward = new Dictionary<int, ExploreRewardData>
                    {
                        {
                            1,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 10,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 10,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 10,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 10,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            2,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 20,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 20,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 20,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 20,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            3,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 30,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 30,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 30,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 30,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            4,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 40,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 40,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 40,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 40,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            5,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 50,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 50,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 50,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 50,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            6,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 60,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 60,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 60,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 60,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            7,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 70,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 70,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 70,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 70,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            8,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 80,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 80,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 80,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 80,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            9,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 90,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 90,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 90,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 90,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            10,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 100,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 100,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 100,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 100,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            11,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 110,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 110,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 110,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 110,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            12,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 120,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 120,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 120,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 120,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            13,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 130,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 130,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 130,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 130,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            14,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 140,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 140,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 140,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 140,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            15,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 150,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 150,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 150,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 150,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            16,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 160,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 160,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 160,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 160,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            17,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 170,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 170,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 170,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 170,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            18,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 180,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 180,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 180,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 180,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            19,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 190,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 190,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 190,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 190,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                        {
                            20,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 200,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 200,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 200,
                                        Ratio = 60,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 200,
                                        Ratio = 40,
                                    },
                                },
                            }
                        },
                    }.ToImmutableDictionary(),
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
                    },
                    exploreReward = new Dictionary<int, ExploreRewardData>
                    {
                        {
                            1,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 10,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 10,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 10,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 10,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 10,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 10,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            2,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 20,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 20,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 20,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 20,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 20,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 20,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            3,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 30,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 30,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 30,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 30,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 30,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 30,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            4,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 40,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 40,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 40,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 40,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 40,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 40,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            5,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 50,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 50,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 50,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 50,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 50,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 50,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            6,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 60,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 60,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 60,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 60,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 60,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 60,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            7,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 70,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 70,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 70,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 70,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 70,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 70,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            8,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 80,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 80,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 80,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 80,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 80,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 80,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            9,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 90,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 90,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 90,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 90,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 90,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 90,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            10,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 100,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 100,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 100,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 100,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 100,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 100,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            11,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 110,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 110,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 110,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 110,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 110,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 110,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            12,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 120,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 120,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 120,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 120,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 120,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 120,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            13,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 130,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 130,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 130,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 130,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 130,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 130,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            14,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 140,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 140,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 140,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 140,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 140,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 140,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            15,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 150,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 150,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 150,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 150,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 150,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 150,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            16,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 160,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 160,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 160,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 160,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 160,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 160,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            17,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 170,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 170,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 170,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 170,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 170,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 170,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            18,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 180,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 180,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 180,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 180,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 180,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 180,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            19,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 190,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 190,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 190,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 190,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 190,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 190,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            20,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 200,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 200,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 200,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 200,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 200,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 200,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                    }.ToImmutableDictionary(),
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
                    },
                    exploreReward = new Dictionary<int, ExploreRewardData>
                    {
                        {
                            1,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 10,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 10,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 10,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 10,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 10,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 10,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            2,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 20,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 20,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 20,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 20,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 20,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 20,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            3,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 30,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 30,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 30,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 30,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 30,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 30,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            4,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 40,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 40,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 40,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 40,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 40,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 40,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            5,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 50,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 50,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 50,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 50,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 50,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 50,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            6,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 60,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 60,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 60,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 60,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 60,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 60,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            7,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 70,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 70,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 70,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 70,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 70,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 70,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            8,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 80,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 80,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 80,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 80,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 80,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 80,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            9,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 90,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 90,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 90,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 90,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 90,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 90,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            10,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 100,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 100,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 100,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 100,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 100,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 100,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            11,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 110,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 110,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 110,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 110,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 110,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 110,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            12,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 120,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 120,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 120,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 120,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 120,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 120,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            13,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 130,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 130,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 130,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 130,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 130,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 130,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            14,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 140,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 140,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 140,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 140,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 140,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 140,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            15,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 150,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 150,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 150,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 150,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 150,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 150,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            16,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 160,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 160,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 160,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 160,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 160,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 160,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            17,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 170,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 170,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 170,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 170,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 170,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 170,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            18,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 180,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 180,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 180,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 180,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 180,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 180,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            19,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 190,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 190,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 190,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 190,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 190,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 190,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                        {
                            20,
                            new ExploreRewardData
                            {
                                FirstReward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 20001, Amount = 200,
                                        Ratio = 50,
                                    },
                                    new ()
                                    {
                                        RewardType = "Rune", RewardId = 30001, Amount = 200,
                                        Ratio = 50,
                                    },
                                },
                                Reward = new ExploreReward[]
                                {
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600301, Amount = 200,
                                        Ratio = 40,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600302, Amount = 200,
                                        Ratio = 30,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600303, Amount = 200,
                                        Ratio = 20,
                                    },
                                    new ()
                                    {
                                        RewardType = "Material", RewardId = 600304, Amount = 200,
                                        Ratio = 10
                                    },
                                },
                            }
                        },
                    }.ToImmutableDictionary(),
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
