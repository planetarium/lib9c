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
        public int MaxFloor = 5;
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

        public Explorer(IValue bencoded)
        {
            var list = (List)bencoded;
            AvatarAddress = list[0].ToAddress();
            Score = (Integer)list[1];
            Floor = (Integer)list[2];
            MaxFloor = (Integer)list[3];
            UsedApPotion = (Integer)list[4];
            UsedGoldenDust = (Integer)list[5];
            UsedNcg = (Integer)list[6];
            Claimed = list[7].ToBoolean();
        }

        public IValue Bencoded => List.Empty
            .Add(AvatarAddress.Serialize())
            .Add(Score).Add(Floor).Add(MaxFloor)
            .Add(UsedApPotion).Add(UsedGoldenDust).Add(UsedNcg)
            .Add(Claimed.Serialize());
    }
}
