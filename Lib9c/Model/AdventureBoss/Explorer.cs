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
        public bool Claimed;
        public FungibleAssetValue? UsedNcg;


        public Explorer(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
            Score = 0;
            Floor = 0;
            Claimed = false;
            UsedNcg = null;
        }

        public Explorer(Address avatarAddress, int score, int floor)
        {
            AvatarAddress = avatarAddress;
            Score = score;
            Floor = floor;
            Claimed = false;
            UsedNcg = null;
        }

        public Explorer(IValue bencoded)
        {
            var list = (List)bencoded;
            AvatarAddress = list[0].ToAddress();
            Score = list[1].ToInteger();
            Floor = list[2].ToInteger();
            UsedApPotion = list[3].ToInteger();
            UsedGoldenDust = list[4].ToInteger();
            Claimed = list[5].ToBoolean();
            if (list.Count > 6)
            {
                UsedNcg = list[6].ToFungibleAssetValue();
            }
        }

        private IValue _bencoded()
        {
            var bencoded = List.Empty
                .Add(AvatarAddress.Serialize())
                .Add(Score.Serialize())
                .Add(Floor.Serialize())
                .Add(UsedApPotion.Serialize())
                .Add(UsedGoldenDust.Serialize())
                .Add(Claimed.Serialize());
            if (UsedNcg is not null)
            {
                bencoded = bencoded.Add(UsedNcg.Serialize());
            }

            return bencoded;
        }

        public IValue Bencoded => _bencoded();
    }
}
