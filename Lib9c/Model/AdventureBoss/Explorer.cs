using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Model.State;

namespace Nekoyume.Model.AdventureBoss
{
    public class Explorer : IBencodable
    {
        public Address AvatarAddress;
        public int Score;
        public int Floor;
        public int MaxFloor = 5;
        public int UsedApPotion;
        public int UsedGoldenDust;
        public int UsedNcg;
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
            MaxFloor = list[3].ToInteger();
            UsedApPotion = list[4].ToInteger();
            UsedGoldenDust = list[5].ToInteger();
            UsedNcg = list[6].ToInteger();
            Claimed = list[7].ToBoolean();
        }

        public IValue Bencoded => List.Empty
            .Add(AvatarAddress.Serialize())
            .Add(Score.Serialize())
            .Add(Floor.Serialize())
            .Add(MaxFloor.Serialize())
            .Add(UsedApPotion.Serialize())
            .Add(UsedGoldenDust.Serialize())
            .Add(UsedNcg.Serialize())
            .Add(Claimed.Serialize());
    }
}
