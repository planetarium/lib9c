using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Lib9c.Model.State;
using Libplanet.Crypto;

namespace Lib9c.Model.AdventureBoss
{
    public class Explorer : IBencodable
    {
        public Address AvatarAddress;
        public string Name;
        public int Score;
        public int Floor;
        public int MaxFloor = 5;
        public int UsedApPotion;
        public int UsedGoldenDust;
        public BigInteger UsedNcg;
        public bool Claimed;


        public Explorer(Address avatarAddress, string name)
        {
            AvatarAddress = avatarAddress;
            Name = name;
            Score = 0;
            Floor = 0;
            Claimed = false;
        }

        public Explorer(IValue bencoded)
        {
            var list = (List)bencoded;
            AvatarAddress = list[0].ToAddress();
            Name = list[1].ToDotnetString();
            Score = (Integer)list[2];
            Floor = (Integer)list[3];
            MaxFloor = (Integer)list[4];
            UsedApPotion = (Integer)list[5];
            UsedGoldenDust = (Integer)list[6];
            UsedNcg = (Integer)list[7];
            Claimed = list[8].ToBoolean();
        }

        public IValue Bencoded => List.Empty
            .Add(AvatarAddress.Serialize()).Add((Text)Name)
            .Add(Score).Add(Floor).Add(MaxFloor)
            .Add(UsedApPotion).Add(UsedGoldenDust).Add(UsedNcg)
            .Add(Claimed.Serialize());
    }
}
