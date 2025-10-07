using System.Numerics;
using Bencodex.Types;
using Lib9c.Helper;
using Lib9c.Model.State;
using Lib9c.TableData.AdventureBoss;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Model.AdventureBoss
{
    public class ExploreBoard
    {
        public long Season;
        public int ExplorerCount;

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
            ExplorerCount = (Integer)bencoded[1];
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

        public void SetReward(AdventureBossContributionRewardSheet.Row rewardInfo, IRandom random)
        {
            (FixedRewardItemId, FixedRewardFavId) =
                AdventureBossHelper.PickReward(random, rewardInfo.Rewards);
        }


        public IValue Bencoded()
        {
            var bencoded = List.Empty
                .Add(Season).Add(ExplorerCount)
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
