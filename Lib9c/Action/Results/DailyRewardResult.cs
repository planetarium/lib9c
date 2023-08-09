using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Results
{
    [Serializable]
    public class DailyRewardResult : AttachmentActionResult
    {
        public Dictionary<Material, int> materials;
        public Guid id;

        protected override string TypeId => "dailyReward.dailyRewardResult";

        public DailyRewardResult()
        {
        }

        public DailyRewardResult(Bencodex.Types.Dictionary serialized) : base(serialized)
        {
            materials = serialized["materials"].ToDictionary_Material_int();
            id = serialized["id"].ToGuid();
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "materials"] = materials.Serialize(),
                [(Text) "id"] = id.Serialize(),
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
