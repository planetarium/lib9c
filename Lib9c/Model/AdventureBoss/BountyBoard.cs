using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.AdventureBoss;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Model.State;
using BigInteger = System.Numerics.BigInteger;

namespace Nekoyume.Model.AdventureBoss
{
    public class BountyBoard
    {
        public const double RaffleRewardRatio = 0.05;

        public int Season;
        public List<Investor> Investors = new ();
        public int? FixedRewardItemId;
        public int? FixedRewardFavTicker;
        public int? RandomRewardItemId;
        public int? RandomRewardFavTicker;
        public Address? RaffleWinner;
        public FungibleAssetValue? RaffleReward;

        public BountyBoard(long season)
        {
        }

        public BountyBoard(List bencoded)
        {
            Season = bencoded[0].ToInteger();
            Investors = bencoded[1].ToList(i => new Investor(i));
            FixedRewardItemId = bencoded[2].ToNullableInteger();
            FixedRewardFavTicker = bencoded[3].ToNullableInteger();
            RandomRewardItemId = bencoded[4].ToNullableInteger();
            RandomRewardFavTicker = bencoded[5].ToNullableInteger();
            if (bencoded.Count > 6)
            {
                RaffleWinner = bencoded[6].ToAddress();
                RaffleReward = bencoded[7].ToFungibleAssetValue();
            }
        }

        public FungibleAssetValue totalBounty()
        {
            FungibleAssetValue totalBounty = default;

            return Investors.Aggregate(totalBounty, (current, inv) => current + inv.Price);
        }

        public void SetReward(WantedReward wantedReward, IRandom random)
        {
            if (wantedReward.FixedRewardItemIdList.Length > 0)
            {
                FixedRewardItemId =
                    wantedReward.FixedRewardItemIdList[
                        random.Next(0, wantedReward.FixedRewardItemIdList.Length)
                    ];
            }

            if (wantedReward.FixedRewardFavTickerList.Length > 0)
            {
                FixedRewardFavTicker =
                    wantedReward.FixedRewardFavTickerList[
                        random.Next(0, wantedReward.FixedRewardFavTickerList.Length)
                    ];
            }

            if (wantedReward.RandomRewardItemIdList.Length > 0)
            {
                RandomRewardItemId =
                    wantedReward.RandomRewardItemIdList[
                        random.Next(0, wantedReward.RandomRewardItemIdList.Length)
                    ];
            }

            if (wantedReward.RandomRewardFavTickerList.Length > 0)
            {
                RandomRewardFavTicker =
                    wantedReward.RandomRewardFavTickerList[
                        random.Next(0, wantedReward.RandomRewardFavTickerList.Length)
                    ];
            }
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
                .Add(FixedRewardFavTicker.Serialize())
                .Add(RandomRewardItemId.Serialize())
                .Add(RandomRewardFavTicker.Serialize());

            if (RaffleWinner is not null)
            {
                bencoded = bencoded.Add(RaffleWinner.Serialize()).Add(RaffleReward.Serialize());
            }

            return bencoded;
        }
    }
}
