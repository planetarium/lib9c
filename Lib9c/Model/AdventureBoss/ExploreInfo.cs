using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Model.State;

namespace Nekoyume.Model.AdventureBoss
{
    public class ExploreInfo : IBencodable
    {
        public Address AvatarAddress;
        public int Score;
        public int Floor;

        public ExploreInfo(Address avatarAddress, int score, int floor)
        {
            AvatarAddress = avatarAddress;
            Score = score;
            Floor = floor;
        }

        public ExploreInfo(IValue bencoded)
        {
            var list = (List)bencoded;
            AvatarAddress = list[0].ToAddress();
            Score = (Integer)list[1];
            Floor = (Integer)list[2];
        }

        public IValue Bencoded => List.Empty
            .Add(AvatarAddress.Serialize())
            .Add(Score)
            .Add(Floor);
    }
}
