using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Results
{
    [Serializable]
    public class BuyerMultipleResult
    {
        public IEnumerable<PurchaseResult> purchaseResults;

        public BuyerMultipleResult()
        {
        }

        public BuyerMultipleResult(Bencodex.Types.Dictionary serialized)
        {
            purchaseResults =
                serialized[SerializeKeys.PurchaseResultsKey]
                    .ToList(StateExtensions.ToPurchaseResult);
        }

        public IValue Serialize() =>
#pragma warning disable LAA1002
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) SerializeKeys.PurchaseResultsKey] = purchaseResults
                    .OrderBy(i => i)
                    .Select(g => g.Serialize()).Serialize()
            });
#pragma warning restore LAA1002
    }
}
