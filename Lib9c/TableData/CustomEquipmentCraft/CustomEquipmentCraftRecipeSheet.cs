using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.CustomEquipmentCraft
{
    [Serializable]
    public class CustomEquipmentCraftRecipeSheet : Sheet<int, CustomEquipmentCraftRecipeSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public ItemSubType ItemSubType { get; private set; }
            public int DrawingAmount { get; private set; }
            public int DrawingToolAmount { get; private set; }
            public long RequiredBlock { get; private set; }
            public StatType FixedStatType { get; private set; }
            public int FixedStatAmount { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ItemSubType = (ItemSubType)Enum.Parse(typeof(ItemSubType), fields[1]);
                DrawingAmount = ParseInt(fields[2]);
                DrawingToolAmount = ParseInt(fields[3]);
                RequiredBlock = ParseLong(fields[4]);
                FixedStatType = (StatType)Enum.Parse(typeof(StatType), fields[5]);
                FixedStatAmount = ParseInt(fields[6]);
            }
        }

        public CustomEquipmentCraftRecipeSheet() : base(nameof(CustomEquipmentCraftRecipeSheet))
        {
        }
    }
}
