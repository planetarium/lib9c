using Bencodex;
using Bencodex.Types;
using Lib9c.Model.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Model.AdventureBoss
{
    public class Investor : IBencodable
    {
        public const int MaxInvestmentCount = 3;
        public Address AvatarAddress;
        public string Name;
        public FungibleAssetValue Price;
        public int Count;
        public bool Claimed;

        public Investor(IValue value)
        {
            List bencoded = (List)value;
            AvatarAddress = bencoded[0].ToAddress();
            Name = bencoded[1].ToDotnetString();
            Price = bencoded[2].ToFungibleAssetValue();
            Count = (Integer)bencoded[3];
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
            .Add((Text)Name)
            .Add(Price.Serialize())
            .Add(Count)
            .Add(Claimed.Serialize());
    }
}
