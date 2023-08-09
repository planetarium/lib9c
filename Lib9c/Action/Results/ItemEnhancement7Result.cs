using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action.Results
{
    [Serializable]
    public class ItemEnhancement7Result : AttachmentActionResult
    {
        protected override string TypeId => "itemEnhancement.result";
        public Guid id;
        public IEnumerable<Guid> materialItemIdList;
        public BigInteger gold;
        public int actionPoint;

        public ItemEnhancement7Result()
        {
        }

        public ItemEnhancement7Result(Dictionary serialized)
            : base(serialized)
        {
            id = serialized["id"].ToGuid();
            materialItemIdList = serialized["materialItemIdList"].ToList(StateExtensions.ToGuid);
            gold = serialized["gold"].ToBigInteger();
            actionPoint = serialized["actionPoint"].ToInteger();
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "id"] = id.Serialize(),
                [(Text) "materialItemIdList"] = materialItemIdList
                    .OrderBy(i => i)
                    .Select(g => g.Serialize()).Serialize(),
                [(Text) "gold"] = gold.Serialize(),
                [(Text) "actionPoint"] = actionPoint.Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        public static BigInteger GetRequiredNCG(EnhancementCostSheet costSheet, int grade, int level)
        {
            var row = costSheet
                .OrderedList
                .FirstOrDefault(x => x.Grade == grade && x.Level == level);

            return row?.Cost ?? 0;
        }

        public static Equipment UpgradeEquipment(Equipment equipment)
        {
            equipment.LevelUpV1();
            return equipment;
        }

        public static int GetRequiredAp()
        {
            return GameConfig.EnhanceEquipmentCostAP;
        }
    }
}