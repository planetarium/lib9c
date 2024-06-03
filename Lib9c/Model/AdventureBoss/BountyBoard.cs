using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Data;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using BigInteger = System.Numerics.BigInteger;

namespace Nekoyume.Model.AdventureBoss
{
    public class BountyBoard
    {
        public long Season;
        public List<Investor> Investors = new ();
        public int? FixedRewardItemId;
        public int? FixedRewardFavId;
        public int? RandomRewardItemId;
        public int? RandomRewardFavId;
        public Address? RaffleWinner;
        public string RaffleWinnerName = "";
        public FungibleAssetValue? RaffleReward;

        public BountyBoard(long season)
        {
            Season = season;
        }

        public BountyBoard(List bencoded)
        {
            Season = bencoded[0].ToLong();
            Investors = bencoded[1].ToList(i => new Investor(i));
            FixedRewardItemId = bencoded[2].ToNullableInteger();
            FixedRewardFavId = bencoded[3].ToNullableInteger();
            RandomRewardItemId = bencoded[4].ToNullableInteger();
            RandomRewardFavId = bencoded[5].ToNullableInteger();
            if (bencoded.Count > 6)
            {
                RaffleWinner = bencoded[6].ToAddress();
                RaffleWinnerName = bencoded[7].ToDotnetString();
                RaffleReward = bencoded[8].ToFungibleAssetValue();
            }
        }

        public FungibleAssetValue totalBounty()
        {
            if (Investors.Count == 0)
            {
                return new FungibleAssetValue();
            }

            var total = new FungibleAssetValue(Investors[0].Price.Currency);
            return Investors.Aggregate(total, (current, inv) => current + inv.Price);
        }

        public void SetReward(AdventureBossGameData.RewardInfo rewardInfo, IRandom random)
        {
            (FixedRewardItemId, FixedRewardFavId) = AdventureBossHelper.PickReward(random,
                rewardInfo.FixedRewardItemIdDict, rewardInfo.FixedRewardFavIdDict);
            (RandomRewardItemId, RandomRewardFavId) = AdventureBossHelper.PickReward(random,
                rewardInfo.RandomRewardItemIdDict, rewardInfo.RandomRewardFavTickerDict);
        }

        public void AddOrUpdate(Address avatarAddress, string name, FungibleAssetValue price)
        {
            var investor = Investors.FirstOrDefault(i => i.AvatarAddress.Equals(avatarAddress));
            if (investor is null)
            {
                Investors.Add(new Investor(avatarAddress, name, price));
            }
            else
            {
                if (investor.Count == Investor.MaxInvestmentCount)
                {
                    throw new MaxInvestmentCountExceededException(
                        $"Avatar {avatarAddress} already invested {investor.Count} times.");
                }

                investor.Price += price;
                investor.Count++;
            }
        }

        public Address PickRaffleWinner(IRandom random)
        {
            if (RaffleWinner is not null)
            {
                return (Address)RaffleWinner;
            }

            var totalBounty = new BigInteger();
            foreach (var inv in Investors)
            {
                totalBounty += inv.Price.RawValue;
            }

            var target = random.Next(0, (int)totalBounty);
            foreach (var inv in Investors)
            {
                if (target < inv.Price.RawValue)
                {
                    RaffleWinner = inv.AvatarAddress;
                    RaffleWinnerName = inv.Name;
                    break;
                }

                target -= (int)inv.Price.RawValue;
            }

            return (Address)RaffleWinner!;
        }

        public IValue Bencoded()
        {
            var bencoded = List.Empty
                .Add(Season.Serialize())
                .Add(new List(Investors.Select(i => i.Bencoded)).Serialize())
                .Add(FixedRewardItemId.Serialize())
                .Add(FixedRewardFavId.Serialize())
                .Add(RandomRewardItemId.Serialize())
                .Add(RandomRewardFavId.Serialize());

            if (RaffleWinner is not null)
            {
                bencoded = bencoded
                    .Add(RaffleWinner.Serialize())
                    .Add((Text)RaffleWinnerName)
                    .Add(RaffleReward.Serialize());
            }

            return bencoded;
        }
    }
}
