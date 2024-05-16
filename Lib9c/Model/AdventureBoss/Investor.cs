using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Model.AdventureBoss
{
    public class Investor : IBencodable
    {
        public const int MaxInvestmentCount = 3;
        public Address AvatarAddress;
        public string Name;
        public FungibleAssetValue Price;
        public int Count;
        public bool Claimed = false;

        public Investor(IValue value)
        {
            List bencoded = (List)value;
            AvatarAddress = bencoded[0].ToAddress();
            Name = bencoded[1].ToString();
            Price = bencoded[2].ToFungibleAssetValue();
            Count = bencoded[3].ToInteger();
            Claimed = bencoded[4].ToBoolean();
        }

        public Investor(Address avatarAddress, string name, FungibleAssetValue price)
        {
            AvatarAddress = avatarAddress;
            Name = name;
            Price = price;
            Count = 1;
            Claimed = false;
        }

        public IValue Bencoded => List.Empty
            .Add(AvatarAddress.Serialize())
            .Add(Name.Serialize())
            .Add(Price.Serialize())
            .Add(Count.Serialize())
            .Add(Claimed.Serialize());
    }
}
