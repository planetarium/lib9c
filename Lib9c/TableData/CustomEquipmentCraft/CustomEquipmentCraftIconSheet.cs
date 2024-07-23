using System;
using System.Collections.Generic;
using Nekoyume.Model.Item;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.CustomEquipmentCraft
{
    [Serializable]
    public class
        CustomEquipmentCraftIconSheet : Sheet<ItemSubType, CustomEquipmentCraftIconSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<ItemSubType>
        {
            public override ItemSubType Key => ItemSubType;

            public ItemSubType ItemSubType { get; private set; }
            public int IconId { get; private set; }
            public int RequiredProficiency { get; private set; }
            public bool RandomOnly { get; private set; }
            public int Ratio { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                ItemSubType = (ItemSubType)Enum.Parse(typeof(ItemSubType), fields[0]);
                IconId = ParseInt(fields[1]);
                RequiredProficiency = ParseInt(fields[2]);
                // Default setting for icon is random only
                RandomOnly = ParseBool(fields[3], true);
                Ratio = ParseInt(fields[4]);
            }
        }

        public CustomEquipmentCraftIconSheet() : base(nameof(CustomEquipmentCraftIconSheet))
        {
        }
    }
}
