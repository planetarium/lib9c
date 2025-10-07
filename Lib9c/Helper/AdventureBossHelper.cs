using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Lib9c.Battle;
using Lib9c.Data;
using Lib9c.Exceptions;
using Lib9c.Model.AdventureBoss;
using Lib9c.Model.Item;
using Lib9c.Model.State;
using Lib9c.Module;
using Lib9c.TableData;
using Lib9c.TableData.AdventureBoss;
using Lib9c.TableData.Item;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Helper
{
    public static class AdventureBossHelper
    {
        public static string GetSeasonAsAddressForm(long season)
        {
            return $"{season:D40}";
        }

        public const int RaffleRewardPercent = 5;

        public const decimal TotalRewardMultiplier = 1.2m;
        public const decimal FixedRewardRatio = 0.7m;
        public const decimal RandomRewardRatio = 1m - FixedRewardRatio;

        public static (int?, int?) PickReward(
            IRandom random,
            IEnumerable<AdventureBossSheet.RewardRatioData> rewardData
        )
        {
            var selector = new WeightedSelector<AdventureBossSheet.RewardRatioData>(random);
            foreach (var item in rewardData)
            {
                selector.Add(item, item.Ratio);
            }

            int? itemId = null;
            int? favId = null;
            var selected = selector.Select(1).First();
            switch (selected.ItemType)
            {
                case "Material":
                    itemId = selected.ItemId;
                    break;
                case "Rune":
                    favId = selected.ItemId;
                    break;
                default:
                    throw new ItemNotFoundException();
            }

            return (itemId, favId);
        }

        private static AdventureBossGameData.ClaimableReward AddReward(
            AdventureBossGameData.ClaimableReward reward, bool isItem, int id,
            int amount)
        {
            if (isItem)
            {
                if (reward.ItemReward.ContainsKey(id))
                {
                    reward.ItemReward[id] += amount;
                }
                else
                {
                    reward.ItemReward[id] = amount;
                }
            }
            else // FAV
            {
                if (reward.FavReward.ContainsKey(id))
                {
                    reward.FavReward[id] += amount;
                }
                else
                {
                    reward.FavReward[id] = amount;
                }
            }

            return reward;
        }

        public static FungibleAssetValue CalculateRaffleReward(BountyBoard bountyBoard)
        {
            return (bountyBoard.totalBounty() * RaffleRewardPercent).DivRem(100, out _);
        }

        public static ExploreBoard PickExploreRaffle(BountyBoard bountyBoard,
            ExploreBoard exploreBoard, ExplorerList explorerList, IRandom random)
        {
            exploreBoard.RaffleReward = CalculateRaffleReward(bountyBoard);

            if (explorerList.Explorers.Count > 0)
            {
                var winner = explorerList.Explorers.ToImmutableSortedSet()[
                    random.Next(explorerList.Explorers.Count)
                ];
                exploreBoard.RaffleWinner = winner.Item1;
                exploreBoard.RaffleWinnerName = winner.Item2;
            }
            else
            {
                exploreBoard.RaffleWinner = new Address();
            }

            return exploreBoard;
        }

        /// <summary>
        /// Calculate reward for adventure boss operators.
        /// This method only calculates reward for given avatar, not actually give rewards.
        /// </summary>
        /// <param name="reward">Claimable reward for this avatar so far.</param>
        /// <param name="bountyBoard">Bounty board for this season. All the reward amount is based on totalBounty on this board.</param>
        /// <param name="avatarAddress">Target avatar address to calculate reward.</param>
        /// <param name="sheet">NCG to reward exchange ratio sheet. Calculate total reward amount based on this sheet.</param>
        /// <param name="ncgRuneRatio">If a reward is rune, use this fixed ratio, not in sheet.</param>
        /// <param name="ncgReward">out value: calculated NCG reward in this function.
        /// We must handle NCG reward separately because NCG reward must be transferred from each season's bounty address.</param>
        /// <returns>Updated Claimable reward after calculation.</returns>
        public static AdventureBossGameData.ClaimableReward CalculateWantedReward(
            AdventureBossGameData.ClaimableReward reward, BountyBoard bountyBoard,
            Address avatarAddress, AdventureBossNcgRewardRatioSheet sheet,
            decimal ncgRuneRatio,
            out FungibleAssetValue ncgReward
        )
        {
            // Initialize ncgReward from bounty because its from bounty.
            ncgReward = 0 * bountyBoard.totalBounty().Currency;
            // Raffle
            if (reward.NcgReward is null)
            {
                reward.NcgReward = ncgReward;
            }
            else
            {
                reward.NcgReward += ncgReward;
            }

            // calculate total reward
            var totalRewardNcg =
                NumberConversionHelper.SafeDecimalToInt32(Math.Floor(
                    (int)bountyBoard.totalBounty().MajorUnit * TotalRewardMultiplier
                ));
            var bonusNcg = totalRewardNcg - bountyBoard.totalBounty().MajorUnit;

            // Calculate total amount based on NCG exchange ratio
            var totalFixedRewardNcg = NumberConversionHelper.SafeDecimalToInt32(Math.Floor(totalRewardNcg * FixedRewardRatio));
            var totalFixedRewardAmount = NumberConversionHelper.SafeDecimalToInt32(Math.Floor(
                bountyBoard.FixedRewardItemId is not null
                    ? totalFixedRewardNcg / sheet[(int)bountyBoard.FixedRewardItemId].Ratio
                    : totalFixedRewardNcg / ncgRuneRatio
            ));

            var totalRandomRewardNcg = NumberConversionHelper.SafeDecimalToInt32(Math.Floor(totalRewardNcg * RandomRewardRatio));
            var totalRandomRewardAmount = NumberConversionHelper.SafeDecimalToInt32(Math.Floor(
                bountyBoard.RandomRewardItemId is not null
                    ? totalRandomRewardNcg / sheet[(int)bountyBoard.RandomRewardItemId].Ratio
                    : totalRandomRewardNcg / ncgRuneRatio
            ));

            // Calculate my reward
            var myInvestment =
                bountyBoard.Investors.First(inv => inv.AvatarAddress == avatarAddress);
            var maxBounty = bountyBoard.Investors.Max(inv => inv.Price.RawValue);
            var maxInvestors = bountyBoard.Investors
                .Where(inv => inv.Price.RawValue == maxBounty)
                .Select(inv => inv.AvatarAddress).ToList();

            var finalPortion = (decimal)myInvestment.Price.MajorUnit;
            if (maxInvestors.Contains(avatarAddress))
            {
                finalPortion += (decimal)(bonusNcg / maxInvestors.Count);
            }

            var fixedRewardAmount =
                NumberConversionHelper.SafeDecimalToInt32(Math.Floor(totalFixedRewardAmount * finalPortion / totalRewardNcg));

            if (fixedRewardAmount > 0)
            {
                reward = AddReward(reward, bountyBoard.FixedRewardItemId is not null,
                    (int)(bountyBoard.FixedRewardItemId ?? bountyBoard.FixedRewardFavId)!,
                    fixedRewardAmount);
            }

            var randomRewardAmount =
                NumberConversionHelper.SafeDecimalToInt32(Math.Floor(totalRandomRewardAmount * finalPortion / totalRewardNcg));
            if (randomRewardAmount > 0)
            {
                reward = AddReward(reward, bountyBoard.RandomRewardItemId is not null,
                    (int)(bountyBoard.RandomRewardItemId ?? bountyBoard.RandomRewardFavId)!,
                    randomRewardAmount);
            }

            return reward;
        }

        public static bool CollectWantedReward(
            AdventureBossGameData.ClaimableReward reward,
            GameConfigState gameConfig, AdventureBossNcgRewardRatioSheet ncgRewardRatioSheet,
            SeasonInfo seasonInfo, BountyBoard bountyBoard, Investor investor,
            long currentBlockIndex, Address avatarAddress,
            ref AdventureBossGameData.ClaimableReward updatedReward
        )
        {
            // Stop when met claim expired season
            if (seasonInfo.EndBlockIndex + gameConfig.AdventureBossClaimInterval <
                currentBlockIndex)
            {
                return false;
            }

            // If `Claimed` found, all prev. season's rewards already been claimed. Stop here.
            if (investor.Claimed)
            {
                return false;
            }

            // Calculate reward for this season
            reward = CalculateWantedReward(reward, bountyBoard, avatarAddress,
                ncgRewardRatioSheet,
                gameConfig.AdventureBossNcgRuneRatio,
                out var ncgReward
            );

            updatedReward = reward;
            return true;
        }

        /// <summary>
        /// Calculate reward for adventure boss explorers.
        /// This method only calculates reward for given avatar, not actually give rewards.
        /// </summary>
        /// <param name="reward">Claimable reward for this avatar so far.</param>
        /// <param name="bountyBoard">Bounty board for this season. NCG reward is based on totalBounty on this board.</param>
        /// <param name="exploreBoard">Explore board for this season. Total reward amount is base on usedApPotion on this board.</param>
        /// <param name="explorer">Target explorer to calculate reward</param>
        /// <param name="avatarAddress">Target avatar address to calculate reward.</param>
        /// <param name="sheet">NCG to reward exchange ratio sheet. Calculate total reward amount based on this sheet.</param>
        /// <param name="ncgApRatio">Exchange ratio between used AP potion to NCG. Used to set total reward amount.</param>
        /// <param name="ncgRuneRatio">If a reward is rune, use this fixed ratio, not in sheet.</param>
        /// <param name="isReal"></param>
        /// <param name="ncgReward">out value: calculated NCG reward in this function.
        /// We must handle NCG reward separately because NCG reward must be transferred from each season's bounty address.</param>
        /// <returns>Updated Claimable reward after calculation.</returns>
        public static AdventureBossGameData.ClaimableReward CalculateExploreReward(
            AdventureBossGameData.ClaimableReward reward,
            BountyBoard bountyBoard, ExploreBoard exploreBoard,
            Explorer explorer, Address avatarAddress,
            AdventureBossNcgRewardRatioSheet sheet,
            decimal ncgApRatio, decimal ncgRuneRatio,
            bool isReal, out FungibleAssetValue ncgReward)
        {
            var gold = bountyBoard.totalBounty().Currency;
            ncgReward = 0 * gold;

            // Raffle
            if (isReal && exploreBoard.RaffleWinner == avatarAddress)
            {
                ncgReward = (FungibleAssetValue)exploreBoard.RaffleReward!;
            }

            if (reward.NcgReward is null)
            {
                reward.NcgReward = ncgReward;
            }
            else
            {
                reward.NcgReward += ncgReward;
            }

            // calculate ncg reward
            var totalNcgReward = (bountyBoard.totalBounty() * 30).DivRem(100, out _);
            var myNcgReward = exploreBoard.TotalPoint == 0
                ? 0 * totalNcgReward.Currency
                : (totalNcgReward * explorer.Score).DivRem(exploreBoard.TotalPoint, out _);

            // Only > 0.1 NCG will be rewarded.
            if (myNcgReward >= (10 * gold).DivRem(100, out _))
            {
                ncgReward += myNcgReward;
                if (reward.NcgReward is null)
                {
                    reward.NcgReward = myNcgReward;
                }
                else
                {
                    reward.NcgReward += myNcgReward;
                }
            }

            // calculate contribution reward
            var ncgRewardRatio = exploreBoard.FixedRewardItemId is not null
                ? sheet[(int)exploreBoard.FixedRewardItemId].Ratio
                : ncgRuneRatio;
            var totalRewardAmount =
                NumberConversionHelper.SafeDecimalToInt32(Math.Floor(exploreBoard.UsedApPotion * ncgApRatio / ncgRewardRatio));

            var myRewardAmount = 0;
            if (exploreBoard.TotalPoint > 0)
            {
                myRewardAmount = NumberConversionHelper.SafeDecimalToInt32(Math.Floor(
                    (decimal)totalRewardAmount * explorer.Score / exploreBoard.TotalPoint
                ));
            }

            if (myRewardAmount > 0)
            {
                reward = AddReward(reward, exploreBoard.FixedRewardItemId is not null,
                    (int)(exploreBoard.FixedRewardItemId ?? exploreBoard.FixedRewardFavId)!,
                    myRewardAmount
                );
            }

            return reward;
        }

        public static bool CollectExploreReward(
            AdventureBossGameData.ClaimableReward reward,
            GameConfigState gameConfig, AdventureBossNcgRewardRatioSheet ncgRewardRatioSheet,
            SeasonInfo seasonInfo, BountyBoard bountyBoard, ExploreBoard exploreBoard,
            Explorer explorer,
            long currentBlockIndex, Address avatarAddress,
            ref AdventureBossGameData.ClaimableReward updatedReward,
            out FungibleAssetValue ncgReward
        )
        {
            ncgReward = 0 * bountyBoard.totalBounty().Currency;

            // Stop when met claim expired season
            if (seasonInfo.EndBlockIndex + gameConfig.AdventureBossClaimInterval <
                currentBlockIndex)
            {
                return false;
            }

            // If `Claimed` found, all prev. season's rewards already been claimed. Stop here.
            if (explorer.Claimed)
            {
                return false;
            }

            // Calculate reward for this season
            reward = CalculateExploreReward(
                reward, bountyBoard, exploreBoard, explorer, avatarAddress,
                ncgRewardRatioSheet,
                gameConfig.AdventureBossNcgApRatio, gameConfig.AdventureBossNcgRuneRatio,
                isReal: true, out ncgReward
            );

            updatedReward = reward;
            return true;
        }

        public static IWorld AddExploreRewards(IActionContext context, IWorld states,
            Address avatarAddress, Inventory inventory,
            IEnumerable<AdventureBossSheet.RewardAmountData> rewardList)
        {
            foreach (var reward in rewardList)
            {
                switch (reward.ItemType)
                {
                    case "Rune":
                        var runeSheet = states.GetSheet<RuneSheet>();
                        var rune = Currencies.GetRune(runeSheet.OrderedList
                            .First(r => r.Id == reward.ItemId).Ticker);
                        states = states.MintAsset(context, avatarAddress, rune * reward.Amount);
                        break;
                    case "Crystal":
                        states = states.MintAsset(context, avatarAddress,
                            Currencies.Crystal * reward.Amount);
                        break;
                    case "Material":
                        var materialSheet = states.GetSheet<MaterialItemSheet>();
                        var materialRow = materialSheet[reward.ItemId];
                        var material = materialRow.ItemSubType is ItemSubType.Circle
                            ? ItemFactory.CreateTradableMaterial(materialRow)
                            : ItemFactory.CreateMaterial(materialRow);
                        inventory.AddItem(material, reward.Amount);
                        break;
                    case "":
                        // No Item
                        break;
                    default:
                        throw new KeyNotFoundException($"{reward.ItemType} is not valid.");
                }
            }

            return states;
        }
    }
}
