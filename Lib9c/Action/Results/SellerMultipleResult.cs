using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Results
{
    [Serializable]
    public class SellerMultipleResult
    {
        public IEnumerable<SellerResult> sellerResults;

        public SellerMultipleResult()
        {
        }

        public SellerMultipleResult(Bencodex.Types.Dictionary serialized)
        {
            sellerResults = serialized[SerializeKeys.SellerResultsKey].ToList(StateExtensions.ToSellerResult);
        }

        public IValue Serialize() =>
#pragma warning disable LAA1002
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) SerializeKeys.SellerResultsKey] = sellerResults
                    .OrderBy(i => i)
                    .Select(g => g.Serialize()).Serialize()
            });
#pragma warning restore LAA1002
    }
}
