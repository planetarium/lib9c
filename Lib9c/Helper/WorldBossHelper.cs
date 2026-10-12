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
using Nekoyume.Helper;

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

        /// <summary>
        /// Calculates a world boss reward — runes drawn by <see cref="RuneWeightSheet"/>, crystal
        /// and circles — for <paramref name="rank"/> of <paramref name="bossId"/>.
        /// </summary>
        /// <param name="rank">The rank whose reward row is granted.</param>
        /// <param name="bossId">The boss id, as in <c>WorldBossListSheet.boss_id</c>.</param>
        /// <param name="sheet">Weights the drawn runes.</param>
        /// <param name="rewardSheet">
        /// The battle, kill or rank reward sheet the reward row is taken from.
        /// </param>
        /// <param name="runeSheet">Maps a drawn rune id to its currency.</param>
        /// <param name="materialSheet">Supplies the circle material.</param>
        /// <param name="random">Draws the rune count, for a ranged row, and each rune.</param>
        /// <param name="rewardBoost">
        /// The <see cref="BoostScheduleSheet"/> row adjusting this reward at the current block, or
        /// <c>null</c> for none. A caller replaying a reward must pass what the action resolved.
        /// See <see cref="ApplyRewardBoost"/>.
        /// </param>
        /// <returns>The granted assets, runes first and crystal last, and circles.</returns>
        /// <remarks>
        /// <paramref name="rewardBoost"/> scales what was drawn rather than how many draws are made,
        /// so <paramref name="random"/> is consumed exactly as without it and everything drawn after
        /// this reward (e.g. the next kill reward of the same action) stays the same.
        /// </remarks>
        public static (List<FungibleAssetValue> assets, Dictionary<TradableMaterial, int> materials) CalculateReward(
            int rank,
            int bossId,
            RuneWeightSheet sheet,
            IWorldBossRewardSheet rewardSheet,
            RuneSheet runeSheet,
            MaterialItemSheet materialSheet,
            IRandom random,
            BoostScheduleSheet.Row rewardBoost = null
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
                .Select(kv => (runeId: kv.Key, count: ApplyRewardBoost(rewardBoost, kv.Value)))
                .Where(pair => pair.count > 0)
                .Select(pair => RuneHelper.ToFungibleAssetValue(runeSheet[pair.runeId], pair.count))
                .ToList();

            var crystal = ApplyRewardBoost(rewardBoost, rewardRow.Crystal);
            if (crystal > 0)
            {
                assets.Add(crystal * CrystalCalculator.CRYSTAL);
            }

            var materials = new Dictionary<TradableMaterial, int>();
            var circle = ApplyRewardBoost(rewardBoost, rewardRow.Circle);
            if (circle > 0)
            {
                var materialRow =
                    materialSheet.Values.First(r => r.ItemSubType == ItemSubType.Circle);
                var material = ItemFactory.CreateTradableMaterial(materialRow);
                materials.TryAdd(material, 0);
                materials[material] += circle;
            }

            return (assets, materials);
        }

        /// <summary>
        /// Applies a <see cref="BoostScheduleSheet"/> row to one granted amount of a world boss
        /// reward: the count of one drawn rune, the crystal or the circles.
        /// </summary>
        /// <param name="rewardBoost">The row to apply, or <c>null</c> for none.</param>
        /// <param name="amount">The amount the reward row and the draws grant.</param>
        /// <returns>
        /// <paramref name="amount"/> unchanged when there is no row or nothing is granted;
        /// otherwise <see cref="BoostScheduleSheet.Row.Apply"/> of it. A result of 0 means the
        /// amount is not granted at all.
        /// </returns>
        /// <remarks>
        /// A boost adjusts what a reward grants but never adds a kind the row does not grant — a
        /// row with no crystal stays without crystal under <c>ADD</c> — since a reward that exists
        /// only during an event reads as one taken away when the event ends. <c>ADD</c> applies to
        /// every amount separately, i.e. once per distinct rune drawn; use <c>MUL</c> to scale a
        /// reward as a whole.
        /// </remarks>
        public static int ApplyRewardBoost(BoostScheduleSheet.Row rewardBoost, int amount)
        {
            if (rewardBoost is null || amount <= 0)
            {
                return amount;
            }

            return rewardBoost.Apply(amount);
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

            var contribution = myDamage / (decimal)totalDamage * 100;
            contribution = Math.Floor(contribution * 10000) / 10000;
            contribution = Math.Min(contribution, 100m);
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
                var proportionalCount = new BigInteger(countDecimal * contribution * 0.01m);
                if (proportionalCount <= 0)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(ticker))
                {
                    items.Add(new (reward.ItemId, NumberConversionHelper.SafeBigIntegerToInt32(proportionalCount)));
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
