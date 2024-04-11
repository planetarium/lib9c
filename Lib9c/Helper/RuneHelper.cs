#nullable enable
using System.Collections.Generic;
using System.Linq;
using Lib9c;
using Libplanet.Action;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;

namespace Nekoyume.Helper
{
    public static class RuneHelper
    {
        public static readonly Currency StakeRune = Currencies.StakeRune;
        public static readonly Currency DailyRewardRune = Currencies.DailyRewardRune;

        public static Currency ToCurrency(RuneSheet.Row runeRow)
        {
            return Currencies.GetRune(runeRow.Ticker);
        }

        public static FungibleAssetValue ToFungibleAssetValue(
            RuneSheet.Row runeRow,
            int quantity)
        {
            return Currencies.GetRune(runeRow.Ticker) * quantity;
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
            var rewardRow = rewardSheet.OrderedRows.First(r => r.Rank == rank && r.BossId == bossId);
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
                .Select(kv => ToFungibleAssetValue(runeSheet[kv.Key], kv.Value))
                .ToList();

            if (rewardRow.Crystal > 0)
            {
                result.Add(rewardRow.Crystal * CrystalCalculator.CRYSTAL);
            }

            return result;
        }

        public static bool TryEnhancement(
            int startRuneLevel,
            RuneCostSheet.Row costRow,
            IRandom random,
            int tryCount,
            out RuneEnhancement.LevelUpResult levelUpResult)
        {
            levelUpResult = new RuneEnhancement.LevelUpResult();

            for (var i = 0; i < tryCount; i++)
            {
                // No cost Found : throw exception at caller
                if (!costRow.TryGetCost(startRuneLevel + levelUpResult.LevelUpCount + 1,
                        out var cost))
                {
                    return false;
                }

                // Cost burns in every try
                levelUpResult.NcgCost += cost.NcgQuantity;
                levelUpResult.CrystalCost += cost.CrystalQuantity;
                levelUpResult.RuneCost += cost.RuneStoneQuantity;

                if (random.Next(0, GameConfig.MaximumProbability) < cost.LevelUpSuccessRate)
                {
                    levelUpResult.LevelUpCount++;
                }
            }

            return true;
        }

        public static FungibleAssetValue CalculateStakeReward(FungibleAssetValue stakeAmount,
            int rate)
        {
            var (quantity, _) = stakeAmount.DivRem(stakeAmount.Currency * rate);
            return StakeRune * quantity;
        }

        public static int CalculateRuneLevelBonus(AllRuneState allRuneState,
            RuneListSheet runeListSheet, RuneLevelBonusSheet runeLevelBonusSheet)
        {
            var bonusLevel = (from rune in allRuneState.Runes.Values
                let runeRow = runeListSheet.Values.FirstOrDefault(row => row.Id == rune.RuneId)
                where runeRow is not null
                select runeRow.BonusCoef * rune.Level).Sum();

            var bonusRow = runeLevelBonusSheet.Values.OrderByDescending(row => row.RuneLevel)
                .FirstOrDefault(row => row.RuneLevel <= bonusLevel);
            return bonusRow?.Bonus ?? 0;
        }
    }
}
