using System;
using Bencodex.Types;
using Lib9c.Model.Market;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Action
{
    public interface IProductInfo
    {
        public Guid ProductId { get; set; }
        public FungibleAssetValue Price { get; set; }
        public Address AgentAddress { get; set; }
        public Address AvatarAddress { get; set; }
        public ProductType Type { get; set; }

        public IValue Serialize();

        public void ValidateType();
    }
}
