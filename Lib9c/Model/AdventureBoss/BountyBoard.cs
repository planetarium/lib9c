using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Action.Exceptions.AdventureBoss;
using Lib9c.Helper;
using Lib9c.Model.State;
using Lib9c.TableData.AdventureBoss;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Model.AdventureBoss
{
    public class BountyBoard
    {
        public long Season;
        public List<Investor> Investors = new ();
        public int? FixedRewardItemId;
        public int? FixedRewardFavId;
        public int? RandomRewardItemId;
        public int? RandomRewardFavId;

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

        public void SetReward(AdventureBossWantedRewardSheet.Row rewardInfo, IRandom random)
        {
            FixedRewardItemId = rewardInfo.FixedReward.ItemType == "Material"
                ? rewardInfo.FixedReward.ItemId
                : null;
            FixedRewardFavId = rewardInfo.FixedReward.ItemType == "Rune"
                ? rewardInfo.FixedReward.ItemId
                : null;

            (RandomRewardItemId, RandomRewardFavId) = AdventureBossHelper.PickReward(
                random, rewardInfo.RandomRewards
            );
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

        public IValue Bencoded =>
            List.Empty
                .Add(Season.Serialize())
                .Add(new List(Investors.Select(i => i.Bencoded)).Serialize())
                .Add(FixedRewardItemId.Serialize())
                .Add(FixedRewardFavId.Serialize())
                .Add(RandomRewardItemId.Serialize())
                .Add(RandomRewardFavId.Serialize());
    }
}
