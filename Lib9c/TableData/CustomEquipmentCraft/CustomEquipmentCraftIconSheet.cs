using System;
using System.Collections.Generic;
using Lib9c.Model.Item;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.CustomEquipmentCraft
{
    [Serializable]
    public class
        CustomEquipmentCraftIconSheet : Sheet<int, CustomEquipmentCraftIconSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }
            public ItemSubType ItemSubType { get; private set; }
            public int IconId { get; private set; }
            public int RequiredRelationship { get; private set; }
            public bool RandomOnly { get; private set; }
            public int Ratio { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ItemSubType = (ItemSubType)Enum.Parse(typeof(ItemSubType), fields[1]);
                IconId = ParseInt(fields[2]);
                RequiredRelationship = ParseInt(fields[3]);
                // Default setting for icon is random only
                RandomOnly = ParseBool(fields[4], true);
                Ratio = ParseInt(fields[5]);
            }
        }

        public CustomEquipmentCraftIconSheet() : base(nameof(CustomEquipmentCraftIconSheet))
        {
        }
    }
}
