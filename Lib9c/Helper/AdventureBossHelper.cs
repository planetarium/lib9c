using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.AdventureBoss;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Module;
using BigInteger = System.Numerics.BigInteger;

namespace Nekoyume.Helper
{
    public struct ClaimableReward
    {
        public FungibleAssetValue? NcgReward;
        public Dictionary<int, int> ItemReward;
        public Dictionary<int, int> FavReward;
    }


    public static class AdventureBossHelper
    {
        public static string GetSeasonAsAddressForm(long season)
        {
            return $"{season:X40}";
        }

        public const decimal TotalRewardMultiplier = 1.2m;
        public const decimal FixedRewardRatio = 0.7m;
        public const decimal RandomRewardRatio = 1m - FixedRewardRatio;
        public const decimal NcgRuneRatio = 2.5m;

        public static readonly ImmutableDictionary<int, decimal> NcgRewardRatio =
            new Dictionary<int, decimal>
            {
                { 600201, 0.5m }, // Golden Dust : 0.5
                { 600202, 1.5m }, // Ruby Dust : 1.5
                { 600203, 7.5m }, // Diamond Dust : 7.5
                { 600301, 0.1m }, // Normal Hammer : 0.1
                { 600302, 0.5m }, // Rare Hammer : 0.5
                { 600303, 1m }, // Epic Hammer : 1
                { 600304, 4m }, // Unique Hammer : 4
                { 600305, 15m }, // Legendary Hammer : 15
                // { 600306, 60m }, // Divinity Hammer : 60
            }.ToImmutableDictionary();

        public static (int?, int?) PickReward(IRandom random, Dictionary<int, int> itemIdDict,
            Dictionary<int, int> favTickerDict)
        {
            var totalProb = itemIdDict.Values.Sum() + favTickerDict.Values.Sum();
            var target = random.Next(0, totalProb);
            int? itemId = null;
            int? favTicker = null;
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
                foreach (var item in favTickerDict.ToImmutableSortedDictionary())

                {
                    if (target < item.Value)
                    {
                        favTicker = item.Key;
                        break;
                    }

                    target -= item.Value;
                }
            }

            return (itemId, favTicker);
        }

        private static ClaimableReward AddReward(ClaimableReward reward, bool isItem, int id,
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

        public static ClaimableReward CalculateWantedReward(
            ClaimableReward reward, BountyBoard bountyBoard, Address avatarAddress
        )
        {
            // Raffle
            if (bountyBoard.RaffleWinner == avatarAddress)
            {
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

            // Calculate total amount based on NCG exchange ratio
            var totalFixedRewardNcg = (int)Math.Round(totalRewardNcg * FixedRewardRatio);
            var totalFixedRewardAmount = (int)Math.Round(
                bountyBoard.FixedRewardItemId is not null
                    ? totalFixedRewardNcg / NcgRewardRatio[(int)bountyBoard.FixedRewardItemId]
                    : totalFixedRewardNcg / NcgRuneRatio
            );

            var totalRandomRewardNcg = (int)Math.Round(totalRewardNcg * RandomRewardRatio);
            var totalRandomRewardAmount = (int)Math.Round(
                bountyBoard.RandomRewardItemId is not null
                    ? totalRandomRewardNcg / NcgRewardRatio[(int)bountyBoard.RandomRewardItemId]
                    : totalRandomRewardNcg / NcgRuneRatio
            );

            // Calculate my reward
            var myInvestment =
                bountyBoard.Investors.First(inv => inv.AvatarAddress == avatarAddress);
            var maxBounty = bountyBoard.Investors.Max(inv => inv.Price.RawValue);
            var maxInvestors = bountyBoard.Investors
                .Where(inv => inv.Price.RawValue == maxBounty)
                .Select(inv => inv.AvatarAddress).ToList();

            var finalPortion = myInvestment.Price.MajorUnit;
            if (maxInvestors.Contains(avatarAddress))
            {
                finalPortion = myInvestment.Price.MajorUnit *
                               (BigInteger)(1 + 0.2m / maxInvestors.Count);
            }

            reward = AddReward(reward, bountyBoard.FixedRewardItemId is not null,
                (int)(bountyBoard.FixedRewardItemId ?? bountyBoard.FixedRewardFavTicker)!,
                (int)Math.Round((decimal)(totalFixedRewardAmount * finalPortion /
                                          bountyBoard.totalBounty().MajorUnit))
            );

            reward = AddReward(reward, bountyBoard.RandomRewardItemId is not null,
                (int)(bountyBoard.RandomRewardItemId ?? bountyBoard.RandomRewardFavTicker)!,
                (int)Math.Round((decimal)(totalRandomRewardAmount * finalPortion /
                                          bountyBoard.totalBounty().MajorUnit))
            );
            return reward;
        }

        public static IWorld CollectWantedReward(IWorld states, ClaimableReward reward,
            long currentBlockIndex, long season, Address avatarAddress,
            out ClaimableReward collectedReward)
        {
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
                reward = CalculateWantedReward(reward, bountyBoard, avatarAddress);
                investor.Claimed = true;
                states = states.SetBountyBoard(szn, bountyBoard);
            }

            collectedReward = reward;
            return states;
        }

        public static ClaimableReward CalculateExploreReward(ClaimableReward reward,
            BountyBoard bountyBoard, ExploreBoard exploreBoard,
            Explorer explorer, Address avatarAddress)
        {
            // Raffle
            if (exploreBoard.RaffleWinner == avatarAddress)
            {
                reward.NcgReward += (FungibleAssetValue)bountyBoard.RaffleReward!;
            }

            var myContribution = explorer.UsedApPotion / exploreBoard.UsedApPotion;

            // calculate ncg reward
            var gold = bountyBoard.totalBounty().Currency;
            var totalNcgReward = bountyBoard.totalBounty() * (BigInteger)0.15m;
            var myNcgReward = totalNcgReward * myContribution;
            if (myNcgReward >= (BigInteger)0.1m * gold)
            {
                reward.NcgReward += myNcgReward;
            }

            // calculate total reward
            var ncgRewardRatio = exploreBoard.FixedRewardItemId is not null
                ? NcgRewardRatio[(int)exploreBoard.FixedRewardItemId]
                : NcgRuneRatio;
            var totalRewardAmount = (int)Math.Round(exploreBoard.UsedApPotion / ncgRewardRatio);
            reward = AddReward(reward, exploreBoard.FixedRewardItemId is not null,
                (int)(exploreBoard.FixedRewardItemId ?? exploreBoard.FixedRewardFavTicker)!,
                (int)Math.Floor((decimal)(totalRewardAmount * myContribution))
            );
            return reward;
        }

        public static IWorld CollectExploreReward(
            IWorld states, ClaimableReward reward, long currentBlockIndex, long season,
            Address avatarAddress,
            out ClaimableReward collectedReward)
        {
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

                var explorer = states.GetExplorer(season, avatarAddress);

                // If `Claimed` found, all prev. season's rewards already been claimed. Stop here.
                if (explorer.Claimed)
                {
                    break;
                }

                // Calculate reward for this season
                reward = CalculateExploreReward(
                    reward, states.GetBountyBoard(szn), exploreBoard, explorer, avatarAddress
                );

                explorer.Claimed = true;
                states = states.SetExplorer(szn, explorer);
            }

            collectedReward = reward;
            return states;
        }
    }
}
