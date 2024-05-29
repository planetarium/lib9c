using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Data;
using Nekoyume.Helper;
using Nekoyume.Model.State;

namespace Nekoyume.Model.AdventureBoss
{
    public class ExploreBoard
    {
        public long Season;
        public HashSet<Address> ExplorerList = new ();

        public long UsedApPotion;
        public long UsedGoldenDust;
        public int UsedNcg;
        public long TotalPoint;

        public int? FixedRewardItemId;
        public int? FixedRewardFavId;
        public Address? RaffleWinner;
        public FungibleAssetValue? RaffleReward;

        public ExploreBoard(long season)
        {
            Season = season;
        }

        public ExploreBoard(List bencoded)
        {
            Season = bencoded[0].ToLong();
            ExplorerList = bencoded[1].ToHashSet(i => i.ToAddress());
            UsedApPotion = bencoded[2].ToInteger();
            UsedNcg = bencoded[3].ToInteger();
            UsedGoldenDust = bencoded[4].ToInteger();
            TotalPoint = bencoded[5].ToLong();
            FixedRewardItemId = bencoded[6].ToNullableInteger();
            FixedRewardFavId = bencoded[7].ToNullableInteger();
            if (bencoded.Count > 8)
            {
                RaffleWinner = bencoded[8].ToAddress();
                RaffleReward = bencoded[9].ToFungibleAssetValue();
            }
        }

        public void SetReward(AdventureBossData.RewardInfo rewardInfo, IRandom random)
        {
            (FixedRewardItemId, FixedRewardFavId) = AdventureBossHelper.PickReward(random,
                rewardInfo.FixedRewardItemIdDict, rewardInfo.FixedRewardFavIdDict);
        }

        public void AddExplorer(Address avatarAddress)
        {
            ExplorerList.Add(avatarAddress);
        }

        public IValue Bencoded()
        {
            var bencoded = List.Empty
                .Add(Season.Serialize())
                .Add(new List(ExplorerList.OrderBy(e => e).Select(e => e.Serialize())))
                .Add(UsedApPotion.Serialize()).Add(UsedNcg.Serialize()).Add(UsedGoldenDust.Serialize())
                .Add(TotalPoint.Serialize())
                .Add(FixedRewardItemId.Serialize()).Add(FixedRewardFavId.Serialize());
            if (RaffleWinner is not null)
            {
                bencoded = bencoded.Add(RaffleWinner.Serialize()).Add(RaffleReward.Serialize());
            }

            return bencoded;
        }
    }
}
