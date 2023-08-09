using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Results
{
    [Serializable]
    public class CombinationResult : AttachmentActionResult
    {
        public Dictionary<Material, int> materials;
        public Guid id;
        public BigInteger gold;
        public int actionPoint;
        public int recipeId;
        public int? subRecipeId;
        public ItemType itemType;

        protected override string TypeId => "combination.result-model";

        public CombinationResult()
        {
        }

        public CombinationResult(Dictionary serialized) : base(serialized)
        {
            materials = serialized["materials"].ToDictionary_Material_int();
            id = serialized["id"].ToGuid();
            gold = serialized["gold"].ToBigInteger();
            actionPoint = serialized["actionPoint"].ToInteger();
            recipeId = serialized["recipeId"].ToInteger();
            subRecipeId = serialized["subRecipeId"].ToNullableInteger();
            itemType = itemUsable.ItemType;
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "materials"] = materials.Serialize(),
                [(Text) "id"] = id.Serialize(),
                [(Text) "gold"] = gold.Serialize(),
                [(Text) "actionPoint"] = actionPoint.Serialize(),
                [(Text) "recipeId"] = recipeId.Serialize(),
                [(Text) "subRecipeId"] = subRecipeId.Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
