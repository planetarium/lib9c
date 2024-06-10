using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Data;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Nekoyume.Model.AdventureBoss
{
    public class ExploreBoard
    {
        public long Season;
        public HashSet<(Address, string)> ExplorerList = new ();

        public long UsedApPotion;
        public long UsedGoldenDust;
        public BigInteger UsedNcg;
        public long TotalPoint;

        public int? FixedRewardItemId;
        public int? FixedRewardFavId;
        public Address? RaffleWinner;
        public string RaffleWinnerName = "";
        public FungibleAssetValue? RaffleReward;

        public ExploreBoard(long season)
        {
            Season = season;
        }

        public ExploreBoard(List bencoded)
        {
            Season = (Integer)bencoded[0];
            ExplorerList = bencoded[1].ToHashSet(
                i => (((List)i)[0].ToAddress(), ((List)i)[1].ToDotnetString())
            );
            UsedApPotion = (Integer)bencoded[2];
            UsedGoldenDust = (Integer)bencoded[3];
            UsedNcg = (Integer)bencoded[4];
            TotalPoint = (Integer)bencoded[5];
            if (bencoded.Count > 6)
            {
                FixedRewardItemId = bencoded[6].ToNullableInteger();
                FixedRewardFavId = bencoded[7].ToNullableInteger();
            }

            if (bencoded.Count > 8)
            {
                RaffleWinner = bencoded[8].ToAddress();
                RaffleWinnerName = bencoded[9].ToDotnetString();
                RaffleReward = bencoded[10].ToFungibleAssetValue();
            }
        }

        public void SetReward(AdventureBossGameData.RewardInfo rewardInfo, IRandom random)
        {
            (FixedRewardItemId, FixedRewardFavId) = AdventureBossHelper.PickReward(random,
                rewardInfo.FixedRewardItemIdDict, rewardInfo.FixedRewardFavIdDict);
        }

        public void AddExplorer(Address avatarAddress, string name)
        {
            ExplorerList.Add((avatarAddress, name));
        }

        public IValue Bencoded()
        {
            var bencoded = List.Empty
                .Add(Season)
                .Add(new List(ExplorerList.OrderBy(e => e)
                    .Select(e => new List(e.Item1.Serialize(), (Text)e.Item2)))
                )
                .Add(UsedApPotion).Add(UsedGoldenDust).Add(UsedNcg).Add(TotalPoint);

            if (FixedRewardFavId is not null || FixedRewardItemId is not null)
            {
                bencoded = bencoded
                    .Add(FixedRewardItemId.Serialize())
                    .Add(FixedRewardFavId.Serialize());
            }

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