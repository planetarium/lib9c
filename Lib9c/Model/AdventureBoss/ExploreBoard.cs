using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.AdventureBoss;
using Nekoyume.Model.State;

namespace Nekoyume.Model.AdventureBoss
{
    public class ExploreBoard
    {
        public long Season;
        public List<Address> ExplorerList = new ();

        public long UsedApPotion;
        public long UsedGoldenDust;
        public FungibleAssetValue UsedNcg;
        public long TotalPoint;

        public int? FixedRewardItemId;
        public int? FixedRewardFavTicker;
        public Address? RaffleWinner;
        public FungibleAssetValue? RaffleReward;

        public ExploreBoard(long season)
        {
            Season = season;
        }

        public ExploreBoard(List bencoded)
        {
            Season = bencoded[0].ToLong();
            ExplorerList = bencoded[1].ToList(i => i.ToAddress());
            FixedRewardItemId = bencoded[2].ToNullableInteger();
            FixedRewardFavTicker = bencoded[3].ToNullableInteger();
            RaffleWinner = bencoded[4].ToAddress();
            RaffleReward = bencoded[5].ToFungibleAssetValue();
        }

        public void SetReward(RewardInfo rewardInfo, IRandom random)
        {
            var totalProb = rewardInfo.FixedRewardItemIdDict.Values.Sum() +
                            rewardInfo.FixedRewardFavTickerDict.Values.Sum();
            var target = random.Next(0, totalProb);
            foreach (var item in rewardInfo.FixedRewardItemIdDict.ToImmutableSortedDictionary())
            {
                if (target < item.Value)
                {
                    FixedRewardItemId = item.Key;
                    break;
                }

                target -= item.Value;
            }

            if (FixedRewardItemId is null)
            {
                foreach (var item in
                         rewardInfo.FixedRewardFavTickerDict.ToImmutableSortedDictionary())
                {
                    if (target < item.Value)
                    {
                        FixedRewardFavTicker = item.Key;
                        break;
                    }

                    target -= item.Value;
                }
            }
        }

        public IValue Bencoded()
        {
            var bencoded = List.Empty
                .Add(Season.Serialize())
                .Add(new List(ExplorerList.Select(e => e.Serialize())))
                .Add(FixedRewardItemId.Serialize()).Add(FixedRewardFavTicker.Serialize());
            if (RaffleWinner is not null)
            {
                bencoded = bencoded.Add(RaffleWinner.Serialize()).Add(RaffleReward.Serialize());
            }

            return bencoded;
        }
    }
}
