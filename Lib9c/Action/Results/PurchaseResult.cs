using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Results
{
    [Serializable]
    public class PurchaseResult : Results.BuyerResult
    {
        public int errorCode = 0;
        public readonly Guid productId;

        public PurchaseResult(Guid shopProductId)
        {
            productId = shopProductId;
        }

        public PurchaseResult(Bencodex.Types.Dictionary serialized) : base(serialized)
        {
            errorCode = serialized[SerializeKeys.ErrorCodeKey].ToInteger();
            productId = serialized[SerializeKeys.ProductIdKey].ToGuid();
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) SerializeKeys.ErrorCodeKey] = errorCode.Serialize(),
                [(Text) SerializeKeys.ProductIdKey] = productId.Serialize(),
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
