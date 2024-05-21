using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Model.AdventureBoss
{
    public class Explorer : IBencodable
    {
        public Address AvatarAddress;
        public int Score;
        public int Floor;
        public int UsedApPotion;
        public int UsedGoldenDust;
        public FungibleAssetValue UsedNcg;
        public bool Claimed;


        public Explorer(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
            Score = 0;
            Floor = 0;
            Claimed = false;
        }

        public Explorer(Address avatarAddress, int score, int floor)
        {
            AvatarAddress = avatarAddress;
            Score = score;
            Floor = floor;
            Claimed = false;
        }

        public Explorer(IValue bencoded)
        {
            var list = (List)bencoded;
            AvatarAddress = list[0].ToAddress();
            Score = list[1].ToInteger();
            Floor = list[2].ToInteger();
            UsedApPotion = list[3].ToInteger();
            UsedGoldenDust = list[4].ToInteger();
            UsedNcg = list[5].ToFungibleAssetValue();
            Claimed = list[6].ToBoolean();
        }

        public IValue Bencoded => List.Empty
            .Add(AvatarAddress.Serialize())
            .Add(Score.Serialize())
            .Add(Floor.Serialize())
            .Add(UsedApPotion.Serialize())
            .Add(UsedGoldenDust.Serialize())
            .Add(UsedNcg.Serialize())
            .Add(Claimed.Serialize());
    }
}
