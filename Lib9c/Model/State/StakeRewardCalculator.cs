using System;
using System.Collections.Generic;
using System.Numerics;
using Lib9c.Helper;
using Lib9c.Model.Item;
using Lib9c.TableData;
using Lib9c.TableData.Item;
using Libplanet.Action;
using Libplanet.Types.Assets;

namespace Lib9c.Model.State
{
    public static class StakeRewardCalculator
    {
        public static Dictionary<ItemBase, int> CalculateFixedRewards(int stakeLevel, IRandom random, StakeRegularFixedRewardSheet stakeRegularFixedRewardSheet, ItemSheet itemSheet, int rewardSteps)
        {
            var result = new Dictionary<ItemBase, int>();
            foreach (var reward in stakeRegularFixedRewardSheet[stakeLevel].Rewards)
            {
                var itemRow = itemSheet[reward.ItemId];
                var item = itemRow is MaterialItemSheet.Row materialRow
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateItem(itemRow, random);
                var count = reward.Count * rewardSteps;
                result.TryAdd(item, 0);
                result[item] += count;
            }

            return result;
        }

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
                        // NOTE: throw exception if reward.CurrencyTicker is null or empty.
                        catch (ArgumentNullException)
                        {
                            throw;
                        }
                        // NOTE: handle the case that reward.CurrencyTicker isn't covered by
                        //       Currencies.GetMinterlessCurrency().
                        catch (ArgumentException)
                        {
                            // NOTE: throw exception if reward.CurrencyDecimalPlaces is null.
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
