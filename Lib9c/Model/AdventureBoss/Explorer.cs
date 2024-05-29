using System.Numerics;
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
        public int UsedApPotion;
        public int UsedGoldenDust;
        public BigInteger UsedNcg;
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
            Score = (Integer)list[1];
            Floor = (Integer)list[2];
            UsedApPotion = (Integer)list[3];
            UsedGoldenDust = (Integer)list[4];
            UsedNcg = (Integer)list[5];
            Claimed = list[6].ToBoolean();
        }

        public IValue Bencoded => List.Empty
            .Add(AvatarAddress.Serialize())
            .Add(Score)
            .Add(Floor)
            .Add(UsedApPotion)
            .Add(UsedGoldenDust)
            .Add(UsedNcg)
            .Add(Claimed.Serialize());
    }
}
