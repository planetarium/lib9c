using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Results
{
    [Serializable]
    public class RapidCombination5Result : AttachmentActionResult
    {
        public Guid id;
        public Dictionary<Material, int> cost;

        protected override string TypeId => "rapid_combination5.result";

        public RapidCombination5Result(Dictionary serialized) : base(serialized)
        {
            id = serialized["id"].ToGuid();
            if (serialized.TryGetValue((Text) "cost", out var value))
            {
                cost = value.ToDictionary_Material_int();
            }
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "id"] = id.Serialize(),
                [(Text) "cost"] = cost.Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}