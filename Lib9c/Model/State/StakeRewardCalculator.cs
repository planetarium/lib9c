using System;
using System.Collections.Generic;
using System.Numerics;
using Lib9c;
using Libplanet.Action;
using Libplanet.Types.Assets;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Provides methods for calculating staking rewards from reward sheets.
    /// </summary>
    public static class StakeRewardCalculator
    {
        /// <summary>
        /// Calculates fixed item rewards for a given stake level.
        /// Fixed rewards have a predetermined item count (unlike rate-based rewards in
        /// <see cref="CalculateRewards"/>), and the <c>tradable</c> flag on each
        /// <see cref="StakeRegularFixedRewardSheet.RewardInfo"/> controls whether
        /// material items are created as tradable or non-tradable.
        /// </summary>
        /// <param name="stakeLevel">The current stake level used to look up the reward row.</param>
        /// <param name="random">Random source used for non-material item creation.</param>
        /// <param name="stakeRegularFixedRewardSheet">Sheet containing fixed reward definitions.</param>
        /// <param name="itemSheet">Sheet used to look up item metadata by ID.</param>
        /// <param name="rewardSteps">
        /// The number of reward steps accumulated since the last claim.
        /// Each reward's count is multiplied by this value.
        /// </param>
        /// <returns>
        /// A dictionary mapping each rewarded <see cref="ItemBase"/> to its total count.
        /// </returns>
        public static Dictionary<ItemBase, int> CalculateFixedRewards(int stakeLevel, IRandom random, StakeRegularFixedRewardSheet stakeRegularFixedRewardSheet, ItemSheet itemSheet, int rewardSteps)
        {
            var result = new Dictionary<ItemBase, int>();
            foreach (var reward in stakeRegularFixedRewardSheet[stakeLevel].Rewards)
            {
                var itemRow = itemSheet[reward.ItemId];
                // Use reward.Tradable to determine whether to create a tradable or
                // non-tradable material, consistent with StakeRegularRewardSheet behavior.
                ItemBase item;
                if (itemRow is MaterialItemSheet.Row materialRow)
                {
                    item = reward.Tradable
                        ? ItemFactory.CreateTradableMaterial(materialRow)
                        : ItemFactory.CreateMaterial(materialRow);
                }
                else
                {
                    item = ItemFactory.CreateItem(itemRow, random);
                }

                var count = reward.Count * rewardSteps;
                result.TryAdd(item, 0);
                result[item] += count;
            }

            return result;
        }

        /// <summary>
        /// Calculates rate-based item and currency rewards for a given staking level.
        /// Unlike <see cref="CalculateFixedRewards"/>, reward quantities are derived from
        /// the ratio of the staked NCG amount to each reward's
        /// <see cref="StakeRegularRewardSheet.RewardInfo.DecimalRate"/>.
        /// Rewards whose computed quantity is zero or negative are skipped.
        /// </summary>
        /// <param name="ncg">The NCG currency definition.</param>
        /// <param name="stakedNcg">The total amount of staked NCG.</param>
        /// <param name="stakingLevel">The current staking level used to look up the reward row.</param>
        /// <param name="rewardSteps">
        /// The number of reward steps accumulated since the last claim.
        /// Each reward quantity is multiplied by this value.
        /// </param>
        /// <param name="stakeRegularRewardSheet">Sheet containing rate-based reward definitions.</param>
        /// <param name="itemSheet">Sheet used to look up item metadata by ID.</param>
        /// <param name="random">Random source used for non-material item creation.</param>
        /// <returns>
        /// A tuple of:
        /// <list type="bullet">
        ///   <item><description>
        ///     <c>itemResult</c>: a dictionary mapping each rewarded <see cref="ItemBase"/>
        ///     to its total count.
        ///   </description></item>
        ///   <item><description>
        ///     <c>favResult</c>: a list of <see cref="FungibleAssetValue"/> for rune and
        ///     currency rewards.
        ///   </description></item>
        /// </list>
        /// </returns>
        public static (Dictionary<ItemBase, int> itemResult, List<FungibleAssetValue> favResult) CalculateRewards(Currency ncg, FungibleAssetValue stakedNcg, int stakingLevel, int rewardSteps, StakeRegularRewardSheet stakeRegularRewardSheet, ItemSheet itemSheet, IRandom random)
        {
            var stakedNcgDecimal = TableExtensions.ParseDecimal(stakedNcg.GetQuantityString());
            var itemResult = new Dictionary<ItemBase, int>();
            var favResult = new List<FungibleAssetValue>();
            foreach (var reward in stakeRegularRewardSheet[stakingLevel].Rewards)
            {
                var rewardQuantityForSingleStep =
                    new BigInteger(stakedNcgDecimal / reward.DecimalRate);
                if (rewardQuantityForSingleStep <= 0)
                {
                    continue;
                }

                switch (reward.Type)
                {
                    case StakeRegularRewardSheet.StakeRewardType.Item:
                    {
                        var majorUnit = (int) rewardQuantityForSingleStep * rewardSteps;
                        if (majorUnit < 1)
                        {
                            continue;
                        }

                        var itemRow = itemSheet[reward.ItemId];
                        ItemBase item;
                        if (itemRow is MaterialItemSheet.Row materialRow)
                        {
                            item = reward.Tradable
                                ? ItemFactory.CreateTradableMaterial(materialRow)
                                : ItemFactory.CreateMaterial(materialRow);
                        }
                        else
                        {
                            item = ItemFactory.CreateItem(itemRow, random);
                        }
                        itemResult.TryAdd(item, 0);
                        itemResult[item] += majorUnit;
                        break;
                    }
                    case StakeRegularRewardSheet.StakeRewardType.Rune:
                    {
                        var majorUnit = rewardQuantityForSingleStep * rewardSteps;
                        if (majorUnit < 1)
                        {
                            continue;
                        }

                        var runeReward = RuneHelper.StakeRune * majorUnit;
                        favResult.Add(runeReward);
                        break;
                    }
                    case StakeRegularRewardSheet.StakeRewardType.Currency:
                    {
                        // NOTE: prepare reward currency.
                        Currency rewardCurrency;
                        // NOTE: this line covers the reward.CurrencyTicker is following cases:
                        //       - Currencies.Crystal.Ticker
                        //       - Currencies.Garage.Ticker
                        //       - lower case is starting with "rune_" or "runestone_"
                        //       - lower case is starting with "soulstone_"
                        try
                        {
                            rewardCurrency =
                                Currencies.GetMinterlessCurrency(reward.CurrencyTicker);
                        }
                        // NOTE: throw exception if CurrencyTicker is null or empty.
                        catch (ArgumentNullException)
                        {
                            throw;
                        }
                        // NOTE: handle the case that CurrencyTicker isn't covered by
                        //       Currencies.GetMinterlessCurrency().
                        catch (ArgumentException)
                        {
                            // NOTE: throw exception if CurrencyDecimalPlaces is null.
                            if (reward.CurrencyDecimalPlaces is null)
                            {
                                throw new ArgumentNullException(
                                    $"Decimal places of {reward.CurrencyTicker} is null");
                            }

                            // NOTE: new currency is created as uncapped currency.
                            rewardCurrency = Currency.Uncapped(
                                reward.CurrencyTicker,
                                Convert.ToByte(reward.CurrencyDecimalPlaces.Value),
                                minters: null);
                        }

                        var majorUnit = rewardQuantityForSingleStep * rewardSteps;
                        var rewardFav = rewardCurrency * majorUnit;
                        favResult.Add(rewardFav);
                        break;
                    }
                    default:
                        throw new ArgumentException($"Can't handle reward type: {reward.Type}");
                }
            }
            return (itemResult, favResult);
        }
    }
}
