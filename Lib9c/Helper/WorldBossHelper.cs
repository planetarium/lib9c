using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Lib9c;
using Libplanet.Action;
using Libplanet.Types.Assets;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    public static class WorldBossHelper
    {
        [Obsolete("Use GameConfigState.DailyWorldBossInterval")]
        public const long RefillInterval = 7200L;
        public const int MaxChallengeCount = 3;

        public static int CalculateRank(WorldBossCharacterSheet.Row row, long score)
        {
            var rank = 0;
            // Wave stats are already sorted by wave number.
            foreach (var waveData in row.WaveStats)
            {
                score -= (long)waveData.HP;
                if (score < 0)
                {
                    break;
                }
                ++rank;
            }

            return Math.Min(row.WaveStats.Count, rank);
        }

        public static FungibleAssetValue CalculateTicketPrice(WorldBossListSheet.Row row, RaiderState raiderState, Currency currency)
        {
            return (row.TicketPrice + row.AdditionalTicketPrice * raiderState.PurchaseCount) * currency;
        }

        public static bool CanRefillTicketV1(long blockIndex, long refilledIndex, long startedIndex)
        {
            return (blockIndex - startedIndex) / RefillInterval > (refilledIndex - startedIndex) / RefillInterval;
        }

        public static bool CanRefillTicket(long blockIndex, long refilledIndex, long startedIndex, int refillInterval)
        {
            return refillInterval > 0 &&
                   (blockIndex - startedIndex) / refillInterval >
                   (refilledIndex - startedIndex) / refillInterval;
        }

        public static (List<FungibleAssetValue> assets, Dictionary<TradableMaterial, int> materials) CalculateReward(
            int rank,
            int bossId,
            RuneWeightSheet sheet,
            IWorldBossRewardSheet rewardSheet,
            RuneSheet runeSheet,
            MaterialItemSheet materialSheet,
            IRandom random
        )
        {
            var row = sheet.Values.First(r => r.Rank == rank && r.BossId == bossId);
            var rewardRow =
                rewardSheet.OrderedRows.First(r => r.Rank == rank && r.BossId == bossId);
            if (rewardRow is WorldBossKillRewardSheet.Row kr)
            {
                kr.SetRune(random);
            }
            else if (rewardRow is WorldBossBattleRewardSheet.Row rr)
            {
                rr.SetRune(random);
            }

            var total = 0;
            var dictionary = new Dictionary<int, int>();
            var selector = new WeightedSelector<int>(random);
            while (total < rewardRow.Rune)
            {
                foreach (var info in row.RuneInfos)
                {
                    selector.Add(info.RuneId, info.Weight);
                }

                var id = selector.Select(1).First();
                dictionary.TryAdd(id, 0);
                dictionary[id] += 1;

                total++;
            }

#pragma warning disable LAA1002
            var assets = dictionary
#pragma warning restore LAA1002
                .Select(kv => RuneHelper.ToFungibleAssetValue(runeSheet[kv.Key], kv.Value))
                .ToList();

            if (rewardRow.Crystal > 0)
            {
                assets.Add(rewardRow.Crystal * CrystalCalculator.CRYSTAL);
            }

            var materials = new Dictionary<TradableMaterial, int>();
            if (rewardRow.Circle > 0)
            {
                var materialRow =
                    materialSheet.Values.First(r => r.ItemSubType == ItemSubType.Circle);
                var material = ItemFactory.CreateTradableMaterial(materialRow);
                materials.TryAdd(material, 0);
                materials[material] += rewardRow.Circle;
            }

            return (assets, materials);
        }

        /// <summary>
        /// Calculates the contribution percentage based on total damage and individual damage.
        /// </summary>
        /// <param name="totalDamage">The total damage dealt.</param>
        /// <param name="myDamage">The damage dealt by the individual.</param>
        /// <returns>The contribution percentage rounded to four decimal places.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when totalDamage is lower than 0.</exception>
        public static decimal CalculateContribution(BigInteger totalDamage, long myDamage)
        {
            if (totalDamage <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalDamage), "total damage must be greater than 0.");
            }

            var contribution = myDamage / (decimal)totalDamage;
            contribution = Math.Min(Math.Round(contribution, 4), 1m);
            return contribution;
        }

        /// <summary>
        /// Calculates the contribution reward based on the given contribution percentage.
        /// </summary>
        /// <param name="row">The row from the WorldBossContributionRewardSheet containing reward details.</param>
        /// <param name="contribution">The contribution percentage of the player.</param>
        /// <returns>A tuple containing a list of item rewards and a list of fungible asset values.</returns>
        public static (List<(int id, int count)>, List<FungibleAssetValue>)
            CalculateContributionReward(WorldBossContributionRewardSheet.Row row,
                decimal contribution)
        {
            var fav = new List<FungibleAssetValue>();
            var items = new List<(int id, int count)>();
            foreach (var reward in row.Rewards)
            {
                var ticker = reward.Ticker;
                var countDecimal = (decimal) reward.Count;
                var proportionalCount = new BigInteger(countDecimal * contribution);
                if (proportionalCount <= 0)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(ticker))
                {
                    items.Add(new (reward.ItemId, (int)proportionalCount));
                }
                else
                {
                    var currency = Currencies.GetMinterlessCurrency(ticker);
                    var asset = currency * proportionalCount;
                    fav.Add(asset);
                }
            }

            return (items, fav);
        }
    }
}
