using System;
using Bencodex.Types;
using Lib9c.Model.Market;
using Lib9c.Model.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Action
{
    public class FavProductInfo : IProductInfo
    {
        public Guid ProductId { get; set; }
        public FungibleAssetValue Price { get; set; }
        public Address AgentAddress { get; set; }
        public Address AvatarAddress { get; set; }
        public ProductType Type { get; set; }

        public FavProductInfo()
        {
        }

        public FavProductInfo(List serialized)
        {
            ProductId = serialized[0].ToGuid();
            Price = serialized[1].ToFungibleAssetValue();
            AgentAddress = serialized[2].ToAddress();
            AvatarAddress = serialized[3].ToAddress();
            Type = serialized[4].ToEnum<ProductType>();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(ProductId.Serialize())
                .Add(Price.Serialize())
                .Add(AgentAddress.Serialize())
                .Add(AvatarAddress.Serialize())
                .Add(Type.Serialize());
        }

        public void ValidateType()
        {
            if (Type != ProductType.FungibleAssetValue)
            {
                throw new InvalidProductTypeException($"{nameof(FavProductInfo)} does not support {Type}");
            }
        }
    }
}
