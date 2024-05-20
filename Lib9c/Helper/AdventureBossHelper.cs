using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet.Action;
using Libplanet.Types.Assets;

namespace Nekoyume.Helper
{
    public static class AdventureBossHelper
    {
        public const decimal TotalRewardMultiplier = 12_000m;
        public const decimal FixedRewardRatio = 7_000m;
        public const decimal RandomRewardRatio = 10_000m - FixedRewardRatio;
        public const decimal NcgRuneRatio = 25_000m;

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

        public static string GetSeasonAsAddressForm(long season)
        {
            return $"{season:X40}";
        }

        public static (int?, int?) PickReward(IRandom random, Dictionary<int, int> itemIdDict,
            Dictionary<int, int> favTickerDict)
        {
            var totalProb = itemIdDict.Values.Sum() + favTickerDict.Values.Sum();
            var target = random.Next(0, totalProb);
            int? itemId = null;
            int? favTicker = null;
            foreach (var item in itemIdDict.ToImmutableSortedDictionary())
            {
                if (target < item.Value)
                {
                    itemId = item.Key;
                    break;
                }

                target -= item.Value;
            }

            if (itemId is null)
            {
                foreach (var item in favTickerDict.ToImmutableSortedDictionary())

                {
                    if (target < item.Value)
                    {
                        favTicker = item.Key;
                        break;
                    }

                    target -= item.Value;
                }
            }

            return (itemId, favTicker);
        }
    }
}
