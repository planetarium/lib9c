using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
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
            public int ResultEquipmentId { get; private set; }
            public int DrawingAmount { get; private set; }
            public int DrawingToolAmount { get; private set; }
            public long RequiredBlock { get; private set; }
            public List<int> IconList { get; private set; }
            public List<int> SkillList { get; private set; }
            public List<int> SubStatList { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ItemSubType = (ItemSubType)Enum.Parse(typeof(ItemSubType), fields[1]);
                ResultEquipmentId = ParseInt(fields[2]);
                DrawingAmount = ParseInt(fields[3]);
                DrawingToolAmount = ParseInt(fields[4]);
                RequiredBlock = ParseLong(fields[5]);
                IconList = fields[6].Split("|").Select(i => ParseInt(i)).ToList();
                SkillList = fields[7].Split("|").Select(i => ParseInt(i)).ToList();
                SubStatList = fields[8].Split("|").Select(i => ParseInt(i)).ToList();
            }
        }

        public CustomEquipmentCraftRecipeSheet() : base(nameof(CustomEquipmentCraftRecipeSheet))
        {
        }
    }
}
