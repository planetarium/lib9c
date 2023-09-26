using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Results
{
    [Serializable]
    public class RapidCombination0Result : CombinationResult
    {
        public Dictionary<Material, int> cost;

        protected override string TypeId => "rapidCombination.result";

        public RapidCombination0Result(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "cost", out var value))
            {
                cost = value.ToDictionary_Material_int();
            }
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "cost"] = cost.Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
