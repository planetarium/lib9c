using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    public class ProductInfo
    {
        public Guid ProductId;
        public FungibleAssetValue Price;
        public Address AgentAddress;
        public Address AvatarAddress;

        public ProductInfo()
        {
        }

        public ProductInfo(List serialized)
        {
            ProductId = serialized[0].ToGuid();
            Price = serialized[1].ToFungibleAssetValue();
            AgentAddress = serialized[2].ToAddress();
            AvatarAddress = serialized[3].ToAddress();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(ProductId.Serialize())
                .Add(Price.Serialize())
                .Add(AvatarAddress.Serialize())
                .Add(AgentAddress.Serialize());
        }
    }
}
