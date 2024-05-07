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
        public FungibleAssetValue Price;
        public int Count;

        public Investor(IValue value)
        {
            List bencoded = (List)value;
            AvatarAddress = bencoded[0].ToAddress();
            Price = bencoded[1].ToFungibleAssetValue();
            Count = (Integer)bencoded[2];
        }

        public Investor(Address avatarAddress, FungibleAssetValue price)
        {
            AvatarAddress = avatarAddress;
            Price = price;
            Count = 1;
        }

        public IValue Bencoded => List.Empty
            .Add(AvatarAddress.Serialize())
            .Add(Price.Serialize())
            .Add(Count);
    }
}
