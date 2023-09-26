using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action.Results
{
    [Serializable]
    public class ItemEnhancement9Result : AttachmentActionResult
    {
        protected override string TypeId => "item_enhancement9.result";
        public Guid id;
        public IEnumerable<Guid> materialItemIdList;
        public BigInteger gold;
        public int actionPoint;
        public EnhancementResult enhancementResult;
        public ItemUsable preItemUsable;

        public ItemEnhancement9Result()
        {
        }

        public ItemEnhancement9Result(Dictionary serialized) : base(serialized)
        {
            id = serialized["id"].ToGuid();
            materialItemIdList = serialized["materialItemIdList"].ToList(StateExtensions.ToGuid);
            gold = serialized["gold"].ToBigInteger();
            actionPoint = serialized["actionPoint"].ToInteger();
            enhancementResult = serialized["enhancementResult"].ToEnum<ItemEnhancement9Result.EnhancementResult>();
            preItemUsable = serialized.ContainsKey("preItemUsable")
                ? (ItemUsable) ItemFactory.Deserialize((Dictionary) serialized["preItemUsable"])
                : null;
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
                [(Text) "enhancementResult"] = StateExtensions.Serialize((Enum) enhancementResult),
                [(Text) "preItemUsable"] = preItemUsable.Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002

        public enum EnhancementResult
        {
            GreatSuccess = 0,
            Success = 1,
            Fail = 2,
        }

        public static EnhancementResult GetEnhancementResult(EnhancementCostSheetV2.Row row, IRandom random)
        {
            var rand = random.Next(1, GameConfig.MaximumProbability + 1);
            if (rand <= row.GreatSuccessRatio)
            {
                return EnhancementResult.GreatSuccess;
            }

            return rand <= row.GreatSuccessRatio + row.SuccessRatio ? EnhancementResult.Success : EnhancementResult.Fail;
        }

        public static int GetRequiredBlockCount(EnhancementCostSheetV2.Row row, EnhancementResult result)
        {
            switch (result)
            {
                case EnhancementResult.GreatSuccess:
                    return row.GreatSuccessRequiredBlockIndex;
                case EnhancementResult.Success:
                    return row.SuccessRequiredBlockIndex;
                case EnhancementResult.Fail:
                    return row.FailRequiredBlockIndex;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
        }

        public static bool TryGetRow(Equipment equipment, EnhancementCostSheetV2 sheet, out EnhancementCostSheetV2.Row row)
        {
            var grade = equipment.Grade;
            var level = equipment.level + 1;
            var itemSubType = equipment.ItemSubType;
            row = sheet.OrderedList.FirstOrDefault(x => x.Grade == grade  && x.Level == level && x.ItemSubType == itemSubType);
            return row != null;
        }
    }
}
