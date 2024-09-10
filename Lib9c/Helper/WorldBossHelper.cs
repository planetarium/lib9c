using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Libplanet.Types.Assets;
using Nekoyume.Battle;
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

        public static List<FungibleAssetValue> CalculateReward(
            int rank,
            int bossId,
            RuneWeightSheet sheet,
            IWorldBossRewardSheet rewardSheet,
            RuneSheet runeSheet,
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
            while (total < rewardRow.Rune)
            {
                var selector = new WeightedSelector<int>(random);
                foreach (var info in row.RuneInfos)
                {
                    selector.Add(info.RuneId, info.Weight);
                }

                var ids = selector.Select(1);
                foreach (var id in ids)
                {
                    if (dictionary.ContainsKey(id))
                    {
                        dictionary[id] += 1;
                    }
                    else
                    {
                        dictionary[id] = 1;
                    }
                }

                total++;
            }

#pragma warning disable LAA1002
            var result = dictionary
#pragma warning restore LAA1002
                .Select(kv => RuneHelper.ToFungibleAssetValue(runeSheet[kv.Key], kv.Value))
                .ToList();

            if (rewardRow.Crystal > 0)
            {
                result.Add(rewardRow.Crystal * CrystalCalculator.CRYSTAL);
            }

            return result;
        }
    }
}
