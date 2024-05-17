using System.Collections.Generic;
using System.Linq;
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
            ExplorerList = bencoded[1].ToHashSet(i => i.ToAddress());
            FixedRewardItemId = bencoded[2].ToNullableInteger();
            FixedRewardFavTicker = bencoded[3].ToNullableInteger();
            RaffleWinner = bencoded[4].ToAddress();
            RaffleReward = bencoded[5].ToFungibleAssetValue();
        }

        public void SetReward(RewardInfo rewardInfo, IRandom random)
        {
            (FixedRewardItemId, FixedRewardFavTicker) = AdventureBossHelper.PickReward(random,
                rewardInfo.FixedRewardItemIdDict, rewardInfo.FixedRewardFavTickerDict);
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
                .Add(FixedRewardItemId.Serialize()).Add(FixedRewardFavTicker.Serialize());
            if (RaffleWinner is not null)
            {
                bencoded = bencoded.Add(RaffleWinner.Serialize()).Add(RaffleReward.Serialize());
            }

            return bencoded;
        }
    }
}
