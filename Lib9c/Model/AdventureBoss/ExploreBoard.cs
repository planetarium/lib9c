using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.AdventureBoss;
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
        public BigInteger UsedNcg;
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
            UsedApPotion = (Integer)bencoded[2];
            UsedGoldenDust = (Integer)bencoded[3];
            UsedNcg = (Integer)bencoded[5];
            if (bencoded.Count > 5)
            {
                FixedRewardItemId = bencoded[5].ToNullableInteger();
                FixedRewardFavId = bencoded[6].ToNullableInteger();
            }

            if (bencoded.Count > 7)
            {
                RaffleWinner = bencoded[7].ToAddress();
                RaffleReward = bencoded[8].ToFungibleAssetValue();
            }
        }

        public void SetReward(RewardInfo rewardInfo, IRandom random)
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
                .Add(UsedApPotion).Add(UsedGoldenDust).Add(UsedNcg);

            if (FixedRewardFavId is not null || FixedRewardItemId is not null)
            {
                bencoded = bencoded
                    .Add(FixedRewardItemId.Serialize())
                    .Add(FixedRewardFavId.Serialize());
            }

            if (RaffleWinner is not null)
            {
                bencoded = bencoded.Add(RaffleWinner.Serialize()).Add(RaffleReward.Serialize());
            }

            return bencoded;
        }
    }
}
