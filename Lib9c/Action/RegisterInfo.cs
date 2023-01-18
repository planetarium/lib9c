using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    public class RegisterInfo
    {
        public Address AvatarAddress;
        public FungibleAssetValue Price;
        public Guid TradableId;
        public int ItemCount;
        public ProductType Type;

        public RegisterInfo(List serialized)
        {
            AvatarAddress = serialized[0].ToAddress();
            Price = serialized[1].ToFungibleAssetValue();
            TradableId = serialized[2].ToGuid();
            ItemCount = serialized[3].ToInteger();
            Type = serialized[4].ToEnum<ProductType>();
        }

        public RegisterInfo()
        {
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(AvatarAddress.Serialize())
                .Add(Price.Serialize())
                .Add(TradableId.Serialize())
                .Add(ItemCount.Serialize())
                .Add(Type.Serialize());
        }
    }
}
