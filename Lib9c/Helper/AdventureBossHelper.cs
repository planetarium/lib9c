using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Action.AdventureBoss;
using Nekoyume.Data;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.Item;
using Nekoyume.Module;
using Nekoyume.TableData;

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

        public static (int?, int?) PickReward(IRandom random, Dictionary<int, int> itemIdDict,
            Dictionary<int, int> favIdDict)
        {
            var totalProb = itemIdDict.Values.Sum() + favIdDict.Values.Sum();
            var target = random.Next(0, totalProb);
            int? itemId = null;
            int? favId = null;
            foreach (var item in itemIdDict.ToImmutableSortedDictionary())
            {
                if (target < item.Value)
                {
                    itemId = item.Key;
                    break;
                }

                target -= item.Value;
            }

            if (itemId is null)
            {
                foreach (var item in favIdDict.ToImmutableSortedDictionary())

                {
                    if (target < item.Value)
                    {
                        favId = item.Key;
                        break;
                    }

                    target -= item.Value;
                }
            }

            return (itemId, favId);
        }

        private static AdventureBossData.ClaimableReward AddReward(
            AdventureBossData.ClaimableReward reward, bool isItem, int id,
            int amount)
        {
            if (amount == 0)
            {
                return reward;
            }

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
            else
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

        public static IWorld PickRaffleWinner(IWorld states, IActionContext context, long season)
        {
            var random = context.GetRandom();

            for (var szn = season; szn > 0; szn--)
            {
                // Wanted raffle
                var bountyBoard = states.GetBountyBoard(szn);
                if (bountyBoard.RaffleWinner is not null)
                {
                    break;
                }

                bountyBoard.RaffleReward =
                    (bountyBoard.totalBounty() * RaffleRewardPercent).DivRem(100, out _);
                var totalProb = bountyBoard.Investors.Aggregate(new BigInteger(0),
                    (current, inv) => current + inv.Price.RawValue);
                var target = (BigInteger)random.Next((int)totalProb);
                foreach (var inv in bountyBoard.Investors)
                {
                    if (target < inv.Price.RawValue)
                    {
                        bountyBoard.RaffleWinner = inv.AvatarAddress;
                        break;
                    }

                    target -= inv.Price.RawValue;
                }

                states = states.SetBountyBoard(szn, bountyBoard);

                if (states.TryGetExploreBoard(szn, out var exploreBoard))
                {
                    exploreBoard.RaffleReward =
                        (bountyBoard.totalBounty() * RaffleRewardPercent).DivRem(100, out _);

                    if (exploreBoard.ExplorerList.Count > 0)
                    {
                        exploreBoard.RaffleWinner =
                            exploreBoard.ExplorerList.ToImmutableSortedSet()[
                                random.Next(exploreBoard.ExplorerList.Count)
                            ];
                    }
                    else
                    {
                        exploreBoard.RaffleWinner = new Address();
                    }

                    states = states.SetExploreBoard(szn, exploreBoard);
                }
            }

            return states;
        }

        public static AdventureBossData.ClaimableReward CalculateWantedReward(
            AdventureBossData.ClaimableReward reward, BountyBoard bountyBoard,
            Address avatarAddress,
            out FungibleAssetValue ncgReward
        )
        {
            ncgReward = 0 * bountyBoard.RaffleReward!.Value.Currency;
            // Raffle
            if (bountyBoard.RaffleWinner == avatarAddress)
            {
                ncgReward = (FungibleAssetValue)bountyBoard.RaffleReward!;
                if (reward.NcgReward is null)
                {
                    reward.NcgReward = (FungibleAssetValue)bountyBoard.RaffleReward!;
                }
                else
                {
                    reward.NcgReward += (FungibleAssetValue)bountyBoard.RaffleReward!;
                }
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
                    ? totalFixedRewardNcg /
                      AdventureBossData.NcgRewardRatio[(int)bountyBoard.FixedRewardItemId]
                    : totalFixedRewardNcg / AdventureBossData.NcgRuneRatio
            );

            var totalRandomRewardNcg = (int)Math.Round(totalRewardNcg * RandomRewardRatio);
            var totalRandomRewardAmount = (int)Math.Round(
                bountyBoard.RandomRewardItemId is not null
                    ? totalRandomRewardNcg /
                      AdventureBossData.NcgRewardRatio[(int)bountyBoard.RandomRewardItemId]
                    : totalRandomRewardNcg / AdventureBossData.NcgRuneRatio
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

            reward = AddReward(reward, bountyBoard.FixedRewardItemId is not null,
                (int)(bountyBoard.FixedRewardItemId ?? bountyBoard.FixedRewardFavId)!,
                (int)Math.Round(totalFixedRewardAmount * finalPortion / totalRewardNcg)
            );

            reward = AddReward(reward, bountyBoard.RandomRewardItemId is not null,
                (int)(bountyBoard.RandomRewardItemId ?? bountyBoard.RandomRewardFavId)!,
                (int)Math.Round(totalRandomRewardAmount * finalPortion / totalRewardNcg)
            );
            return reward;
        }

        public static IWorld CollectWantedReward(IWorld states, IActionContext context,
            AdventureBossData.ClaimableReward reward, long currentBlockIndex, long season,
            Address avatarAddress,
            out AdventureBossData.ClaimableReward collectedReward)
        {
            var agentAddress = states.GetAvatarState(avatarAddress).agentAddress;
            for (var szn = season; szn > 0; szn--)
            {
                var seasonInfo = states.GetSeasonInfo(szn);

                // Stop when met claim expired season
                if (seasonInfo.EndBlockIndex + ClaimAdventureBossReward.ClaimableDuration <
                    currentBlockIndex)
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
                    out var ncgReward);

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

        public static AdventureBossData.ClaimableReward CalculateExploreReward(
            AdventureBossData.ClaimableReward reward,
            BountyBoard bountyBoard, ExploreBoard exploreBoard,
            Explorer explorer, Address avatarAddress, out FungibleAssetValue ncgReward)
        {
            ncgReward = 0 * exploreBoard.RaffleReward!.Value.Currency;
            // Raffle
            if (exploreBoard.RaffleWinner == avatarAddress)
            {
                ncgReward += (FungibleAssetValue)exploreBoard.RaffleReward;
                if (reward.NcgReward is null)
                {
                    reward.NcgReward = (FungibleAssetValue)exploreBoard.RaffleReward!;
                }
                else
                {
                    reward.NcgReward += (FungibleAssetValue)exploreBoard.RaffleReward!;
                }
            }

            // calculate ncg reward
            var gold = bountyBoard.totalBounty().Currency;
            var totalNcgReward = (bountyBoard.totalBounty() * 15).DivRem(100, out _);
            var myNcgReward = (totalNcgReward * explorer.UsedApPotion)
                .DivRem(exploreBoard.UsedApPotion, out _);

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

            // calculate total reward
            var ncgRewardRatio = exploreBoard.FixedRewardItemId is not null
                ? AdventureBossData.NcgRewardRatio[(int)exploreBoard.FixedRewardItemId]
                : AdventureBossData.NcgRuneRatio;
            var totalRewardAmount = (int)Math.Round(exploreBoard.UsedApPotion / ncgRewardRatio);
            reward = AddReward(reward, exploreBoard.FixedRewardItemId is not null,
                (int)(exploreBoard.FixedRewardItemId ?? exploreBoard.FixedRewardFavId)!,
                (int)Math.Floor(
                    (decimal)totalRewardAmount * explorer.UsedApPotion / exploreBoard.UsedApPotion)
            );

            return reward;
        }

        public static IWorld CollectExploreReward(IWorld states, IActionContext context,
            AdventureBossData.ClaimableReward reward, long currentBlockIndex, long season,
            Address avatarAddress,
            out AdventureBossData.ClaimableReward collectedReward)
        {
            var agentAddress = states.GetAvatarState(avatarAddress).agentAddress;
            for (var szn = season; szn > 0; szn--)
            {
                var seasonInfo = states.GetSeasonInfo(szn);

                // Stop when met claim expired season
                if (seasonInfo.EndBlockIndex + ClaimAdventureBossReward.ClaimableDuration <
                    currentBlockIndex)
                {
                    break;
                }

                var exploreBoard = states.GetExploreBoard(szn);

                // Not explored
                if (!exploreBoard.ExplorerList.Contains(avatarAddress))
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
                reward = CalculateExploreReward(
                    reward, states.GetBountyBoard(szn), exploreBoard, explorer, avatarAddress,
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

                explorer.Claimed = true;
                states = states.SetExplorer(szn, explorer);
            }

            collectedReward = reward;
            return states;
        }

        public static IWorld AddExploreRewards(IActionContext context, IWorld states,
            Address avatarAddress,
            IEnumerable<AdventureBossData.ExploreReward> rewardList)
        {
            foreach (var reward in rewardList)
            {
                switch (reward.RewardType)
                {
                    case "Rune":
                        var runeSheet = states.GetSheet<RuneSheet>();
                        var rune = Currencies.GetRune(runeSheet.OrderedList
                            .First(r => r.Id == reward.RewardId).Ticker);
                        states = states.MintAsset(context, avatarAddress, rune * reward.Amount);
                        break;
                    case "Crystal":
                        states = states.MintAsset(context, avatarAddress,
                            Currencies.Crystal * reward.Amount);
                        break;
                    case "Material":
                        var materialSheet = states.GetSheet<MaterialItemSheet>();
                        var inventory = states.GetInventory(avatarAddress);
                        var material = ItemFactory.CreateMaterial(
                            materialSheet.Values.First(row => row.Id == reward.RewardId)
                        );
                        inventory.AddItem(material, reward.Amount);
                        break;
                }
            }

            return states;
        }
    }
}
