#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Action;
using Lib9c.Model.State;
using Lib9c.TableData;
using Lib9c.TableData.Rune;
using Libplanet.Action;
using Libplanet.Types.Assets;

namespace Lib9c.Helper
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
            var bonusLevel = 0;
            foreach (var rune in allRuneState.Runes.Values)
            {
                var runeRow = runeListSheet.Values.FirstOrDefault(row => row.Id == rune.RuneId);
                if (runeRow is not null)
                {
                    bonusLevel += runeRow.BonusCoef * rune.Level;
                }
            }

            var runeLevelBonus = 0;
            var prevLevel = 0;
            foreach (var row in runeLevelBonusSheet.Values.OrderBy(row => row.RuneLevel))
            {
                runeLevelBonus += (Math.Min(row.RuneLevel, bonusLevel) - prevLevel) * row.Bonus;
                prevLevel = row.RuneLevel;
                if (row.RuneLevel >= bonusLevel)
                {
                    break;
                }
            }

            return runeLevelBonus;
        }

        public static List<RuneOptionSheet.Row.RuneOptionInfo> GetRuneOptions(
            IEnumerable<RuneWeightSheet.RuneInfo> runeInfos,
            AllRuneState runeStates,
            RuneOptionSheet runeOptionSheet)
        {
            var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            foreach (var runeInfo in runeInfos)
            {
                if (!runeStates.TryGetRuneState(runeInfo.RuneId, out var runeState))
                {
                    continue;
                }

                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var optionRow))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.RuneId);
                }

                if (!optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.Level);
                }

                runeOptions.Add(option);
            }

            return runeOptions;
        }

        public static List<RuneOptionSheet.Row.RuneOptionInfo> GetRuneOptions(
            IEnumerable<RuneSlotInfo> runeInfos,
            AllRuneState runeStates,
            RuneOptionSheet runeOptionSheet)
        {
            var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            foreach (var runeInfo in runeInfos)
            {
                if (!runeStates.TryGetRuneState(runeInfo.RuneId, out var runeState))
                {
                    continue;
                }

                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var optionRow))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.RuneId);
                }

                if (!optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.Level);
                }

                runeOptions.Add(option);
            }

            return runeOptions;
        }
    }
}
