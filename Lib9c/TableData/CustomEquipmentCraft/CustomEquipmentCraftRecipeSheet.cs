using System;
using System.Collections.Generic;
using Lib9c.Model.Item;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.CustomEquipmentCraft
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
            public int ScrollAmount { get; private set; }
            public int CircleAmount { get; private set; }
            public long RequiredBlock { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ItemSubType = (ItemSubType)Enum.Parse(typeof(ItemSubType), fields[1]);
                ScrollAmount = ParseInt(fields[2]);
                CircleAmount = ParseInt(fields[3]);
                RequiredBlock = ParseLong(fields[4]);
            }
        }

        public CustomEquipmentCraftRecipeSheet() : base(nameof(CustomEquipmentCraftRecipeSheet))
        {
        }
    }
}
