using System;
using System.Collections.Generic;
using Lib9c.Model.Item;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.CustomEquipmentCraft
{
    [Serializable]
    public class
        CustomEquipmentCraftRecipeSkillSheet : Sheet<int, CustomEquipmentCraftRecipeSkillSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }
            public ItemSubType ItemSubType { get; private set; }
            public int ItemOptionId { get; private set; }
            public int Ratio { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ItemSubType = (ItemSubType)Enum.Parse(typeof(ItemSubType), fields[1]);
                ItemOptionId = ParseInt(fields[2]);
                Ratio = ParseInt(fields[3]);
            }
        }

        public CustomEquipmentCraftRecipeSkillSheet()
            : base(nameof(CustomEquipmentCraftRecipeSkillSheet))
        {
        }
    }
}
