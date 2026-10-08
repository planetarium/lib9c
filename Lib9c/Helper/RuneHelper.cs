#nullable enable
using System;
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

        /// <summary>
        /// Simulates <paramref name="tryCount"/> enhancement tries from
        /// <paramref name="startRuneLevel"/>, summing the cost of every try and drawing one random
        /// number per try for its success.
        /// </summary>
        /// <param name="startRuneLevel">The rune level before the first try.</param>
        /// <param name="costRow">The rune's cost row.</param>
        /// <param name="random">The random source; one draw per try.</param>
        /// <param name="tryCount">The number of tries.</param>
        /// <param name="levelUpResult">The level-ups and the summed cost.</param>
        /// <param name="boostScheduleSheet">
        /// The patched <see cref="BoostScheduleSheet"/>, or <c>null</c> when the chain has none.
        /// Adjusts each try's crystal and rune stone cost; see
        /// <see cref="ApplyEnhancementCostBoost(RuneCostSheet.RuneCostData, int, BoostScheduleSheet, long)"/>.
        /// </param>
        /// <param name="blockIndex">
        /// The block being evaluated, against which <paramref name="boostScheduleSheet"/> rows are
        /// matched. Ignored when <paramref name="boostScheduleSheet"/> is <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>false</c> when a try reaches a level <paramref name="costRow"/> has no cost for.
        /// </returns>
        public static bool TryEnhancement(
            int startRuneLevel,
            RuneCostSheet.Row costRow,
            IRandom random,
            int tryCount,
            out RuneEnhancement.LevelUpResult levelUpResult,
            BoostScheduleSheet? boostScheduleSheet = null,
            long blockIndex = 0)
        {
            levelUpResult = new RuneEnhancement.LevelUpResult();

            for (var i = 0; i < tryCount; i++)
            {
                var targetLevel = startRuneLevel + levelUpResult.LevelUpCount + 1;

                // No cost Found : throw exception at caller
                if (!costRow.TryGetCost(targetLevel, out var cost))
                {
                    return false;
                }

                cost = ApplyEnhancementCostBoost(cost, targetLevel, boostScheduleSheet, blockIndex);

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

        /// <summary>
        /// Applies the <see cref="BoostScheduleSheet.Targets.RuneEnhancementCrystalCost"/> and
        /// <see cref="BoostScheduleSheet.Targets.RuneEnhancementRuneStoneCost"/> rows active at
        /// <paramref name="blockIndex"/> to the cost of one try that reaches
        /// <paramref name="targetLevel"/>.
        /// </summary>
        /// <param name="cost">The sheet's cost for <paramref name="targetLevel"/>.</param>
        /// <param name="targetLevel">
        /// The level the try attempts to reach, matched against <c>target_id</c>.
        /// </param>
        /// <param name="boostScheduleSheet">
        /// The patched <see cref="BoostScheduleSheet"/>, or <c>null</c> when the chain has none.
        /// </param>
        /// <param name="blockIndex">The block being evaluated.</param>
        /// <returns>
        /// <paramref name="cost"/> itself when no row applies; otherwise a copy whose crystal and
        /// rune stone quantities are adjusted by
        /// <see cref="ApplyEnhancementCostBoostQuantity"/>. NCG quantity and
        /// success rate are always copied unchanged.
        /// </returns>
        /// <remarks>
        /// Callers showing a cost — or a maximum try count — should pass every per-level cost
        /// through this so that they agree with <see cref="RuneEnhancement"/>.
        /// </remarks>
        public static RuneCostSheet.RuneCostData ApplyEnhancementCostBoost(
            RuneCostSheet.RuneCostData cost,
            int targetLevel,
            BoostScheduleSheet? boostScheduleSheet,
            long blockIndex)
        {
            if (boostScheduleSheet is null)
            {
                return cost;
            }

            var crystalBoost = boostScheduleSheet.FindActive(
                BoostScheduleSheet.Targets.RuneEnhancementCrystalCost,
                targetLevel,
                blockIndex);
            var runeStoneBoost = boostScheduleSheet.FindActive(
                BoostScheduleSheet.Targets.RuneEnhancementRuneStoneCost,
                targetLevel,
                blockIndex);
            if (crystalBoost is null && runeStoneBoost is null)
            {
                return cost;
            }

            return new RuneCostSheet.RuneCostData(
                cost.LevelStart,
                cost.LevelEnd,
                ApplyEnhancementCostBoostQuantity(runeStoneBoost, cost.RuneStoneQuantity),
                ApplyEnhancementCostBoostQuantity(crystalBoost, cost.CrystalQuantity),
                cost.NcgQuantity,
                cost.LevelUpSuccessRate);
        }

        /// <summary>
        /// Applies one <see cref="BoostScheduleSheet"/> row to one enhancement cost quantity.
        /// </summary>
        /// <param name="boost">The row to apply, or <c>null</c> for none.</param>
        /// <param name="baseCost">The sheet's quantity.</param>
        /// <returns>
        /// The adjusted quantity, kept within <c>[1, baseCost]</c> when <paramref name="baseCost"/>
        /// is positive. A non-positive <paramref name="baseCost"/> is returned unchanged.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Discount only: a row can never raise a cost. Raising one is not an event anyone has
        /// asked for, and it would let a mistyped row push the per-try sum in
        /// <see cref="TryEnhancement"/> past <see cref="int.MaxValue"/>, where it wraps negative
        /// and the cost is skipped instead of charged.
        /// </para>
        /// <para>
        /// At least 1: rounding down a small cost (4 stones × 0.2 = 0.8) would otherwise make the
        /// try free, turning a discount into a giveaway that the row never declared. A free
        /// enhancement has to be configured in <see cref="RuneCostSheet"/> itself. A cost the
        /// sheet already sets to 0 stays 0, so a boost never creates a cost either.
        /// </para>
        /// </remarks>
        public static int ApplyEnhancementCostBoostQuantity(BoostScheduleSheet.Row? boost, int baseCost)
        {
            if (boost is null || baseCost <= 0)
            {
                return baseCost;
            }

            return Math.Min(Math.Max(boost.Apply(baseCost), 1), baseCost);
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
