using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Module;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Battle;
using Nekoyume.Data;
using Nekoyume.Exceptions;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.TableData.AdventureBoss;

namespace Nekoyume.Helper
{
    public static class AdventureBossHelper
    {
        public static string GetSeasonAsAddressForm(long season)
        {
            return $"{season:X40}";
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

        public static IWorld PickRaffleWinner(IWorld states, IActionContext context, long season)
        {
            var random = context.GetRandom();

            for (var szn = season; szn > 0; szn--)
            {
                // Explore raffle
                var bountyBoard = states.GetBountyBoard(szn);
                var exploreBoard = states.GetExploreBoard(szn);
                var explorerList = states.GetExplorerList(szn);
                if (exploreBoard.RaffleWinner is null)
                {
                    exploreBoard =
                        PickExploreRaffle(bountyBoard, exploreBoard, explorerList, random);
                    states = states.SetExploreBoard(szn, exploreBoard);
                }
            }

            return states;
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
                (int)Math.Round(
                    (int)bountyBoard.totalBounty().MajorUnit * TotalRewardMultiplier
                );
            var bonusNcg = totalRewardNcg - bountyBoard.totalBounty().MajorUnit;

            // Calculate total amount based on NCG exchange ratio
            var totalFixedRewardNcg = (int)Math.Round(totalRewardNcg * FixedRewardRatio);
            var totalFixedRewardAmount = (int)Math.Round(
                bountyBoard.FixedRewardItemId is not null
                    ? totalFixedRewardNcg / sheet[(int)bountyBoard.FixedRewardItemId].Ratio
                    : totalFixedRewardNcg / ncgRuneRatio
            );

            var totalRandomRewardNcg = (int)Math.Round(totalRewardNcg * RandomRewardRatio);
            var totalRandomRewardAmount = (int)Math.Round(
                bountyBoard.RandomRewardItemId is not null
                    ? totalRandomRewardNcg / sheet[(int)bountyBoard.RandomRewardItemId].Ratio
                    : totalRandomRewardNcg / ncgRuneRatio
            );

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
                (int)Math.Round(totalFixedRewardAmount * finalPortion / totalRewardNcg);

            if (fixedRewardAmount > 0)
            {
                reward = AddReward(reward, bountyBoard.FixedRewardItemId is not null,
                    (int)(bountyBoard.FixedRewardItemId ?? bountyBoard.FixedRewardFavId)!,
                    fixedRewardAmount);
            }

            var randomRewardAmount =
                (int)Math.Round(totalRandomRewardAmount * finalPortion / totalRewardNcg);
            if (randomRewardAmount > 0)
            {
                reward = AddReward(reward, bountyBoard.RandomRewardItemId is not null,
                    (int)(bountyBoard.RandomRewardItemId ?? bountyBoard.RandomRewardFavId)!,
                    randomRewardAmount);
            }

            return reward;
        }

        public static IWorld CollectWantedReward(IWorld states, IActionContext context,
            AdventureBossGameData.ClaimableReward reward, long currentBlockIndex, long season,
            Address agentAddress, Address avatarAddress, long claimableDuration,
            out AdventureBossGameData.ClaimableReward collectedReward)
        {
            for (var szn = season; szn > 0; szn--)
            {
                var seasonInfo = states.GetSeasonInfo(szn);

                // Stop when met claim expired season
                if (seasonInfo.EndBlockIndex + claimableDuration < currentBlockIndex)
                {
                    break;
                }

                var bountyBoard = states.GetBountyBoard(szn);
                var investor = bountyBoard.Investors.FirstOrDefault(
                    inv => inv.AvatarAddress == avatarAddress
                );

                // Not invested in this season
                if (investor is null)
                {
                    continue;
                }

                // If `Claimed` found, all prev. season's rewards already been claimed. Stop here.
                if (investor.Claimed)
                {
                    break;
                }

                // Calculate reward for this season
                reward = CalculateWantedReward(reward, bountyBoard, avatarAddress,
                    states.GetSheet<AdventureBossNcgRewardRatioSheet>(),
                    states.GetGameConfigState().AdventureBossNcgRuneRatio,
                    out var ncgReward
                );

                // Transfer NCG reward from seasonal address
                if (ncgReward.RawValue > 0)
                {
                    states = states.TransferAsset(context,
                        Addresses.BountyBoard.Derive(
                            AdventureBossHelper.GetSeasonAsAddressForm(szn)
                        ),
                        agentAddress,
                        ncgReward
                    );
                }

                investor.Claimed = true;
                states = states.SetBountyBoard(szn, bountyBoard);
            }

            collectedReward = reward;
            return states;
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
            var totalNcgReward = (bountyBoard.totalBounty() * 15).DivRem(100, out _);
            var myNcgReward = (totalNcgReward * explorer.Score)
                .DivRem(exploreBoard.TotalPoint, out _);

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
                (int)Math.Round(exploreBoard.UsedApPotion * ncgApRatio / ncgRewardRatio);
            var myRewardAmount = (int)Math.Floor(
                (decimal)totalRewardAmount * explorer.Score / exploreBoard.TotalPoint
            );
            if (myRewardAmount > 0)
            {
                reward = AddReward(reward, exploreBoard.FixedRewardItemId is not null,
                    (int)(exploreBoard.FixedRewardItemId ?? exploreBoard.FixedRewardFavId)!,
                    myRewardAmount
                );
            }

            return reward;
        }

        public static IWorld CollectExploreReward(IWorld states, IActionContext context,
            AdventureBossGameData.ClaimableReward reward, long currentBlockIndex, long season,
            Address agentAddress, Address avatarAddress, long claimableDuration,
            out AdventureBossGameData.ClaimableReward collectedReward)
        {
            for (var szn = season; szn > 0; szn--)
            {
                var seasonInfo = states.GetSeasonInfo(szn);

                // Stop when met claim expired season
                if (seasonInfo.EndBlockIndex + claimableDuration < currentBlockIndex)
                {
                    break;
                }

                var exploreBoard = states.GetExploreBoard(szn);
                var explorerList = states.GetExplorerList(szn);

                // Not explored
                if (!explorerList.Explorers.OrderBy(e => e.Item1)
                        .Select(e => e.Item1)
                        .Contains(avatarAddress)
                   )
                {
                    continue;
                }

                var explorer = states.GetExplorer(szn, avatarAddress);

                // If `Claimed` found, all prev. season's rewards already been claimed. Stop here.
                if (explorer.Claimed)
                {
                    break;
                }

                // Calculate reward for this season
                var gameConfig = states.GetGameConfigState();
                reward = CalculateExploreReward(
                    reward, states.GetBountyBoard(szn), exploreBoard, explorer, avatarAddress,
                    states.GetSheet<AdventureBossNcgRewardRatioSheet>(),
                    gameConfig.AdventureBossNcgApRatio, gameConfig.AdventureBossNcgRuneRatio,
                    isReal: true, out var ncgReward
                );

                // Transfer NCG reward from seasonal address
                if (ncgReward.RawValue > 0)
                {
                    states = states.TransferAsset(context,
                        Addresses.BountyBoard.Derive(
                            AdventureBossHelper.GetSeasonAsAddressForm(szn)
                        ),
                        agentAddress,
                        ncgReward
                    );
                }

                explorer.Claimed = true;
                states = states.SetExplorer(szn, explorer);
            }

            collectedReward = reward;
            return states;
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
                        var material = ItemFactory.CreateMaterial(
                            materialSheet.Values.First(row => row.Id == reward.ItemId)
                        );
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
