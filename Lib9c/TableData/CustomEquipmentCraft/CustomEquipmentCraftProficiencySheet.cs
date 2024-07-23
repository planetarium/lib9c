using System;
using System.Collections.Generic;
using System.Numerics;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.CustomEquipmentCraft
{
    [Serializable]
    public class
        CustomEquipmentCraftProficiencySheet : Sheet<int, CustomEquipmentCraftProficiencySheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }
            public int Proficiency { get; private set; }
            public decimal CostMultiplier { get; private set; }
            public int MinCp { get; private set; }
            public int MaxCp { get; private set; }
            public int EquipmentRequiredLevel { get; private set; }
            public int WeaponItemId { get; private set; }
            public int ArmorItemId { get; private set; }
            public int BeltItemId { get; private set; }
            public int NecklaceItemId { get; private set; }
            public int RingItemId { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                Proficiency = ParseInt(fields[1]);
                CostMultiplier = ParseDecimal(fields[2]);
                MinCp = ParseInt(fields[3]);
                MaxCp = ParseInt(fields[4]);
                EquipmentRequiredLevel = ParseInt(fields[5]);
                WeaponItemId = ParseInt(fields[6]);
                ArmorItemId = ParseInt(fields[7]);
                BeltItemId = ParseInt(fields[8]);
                NecklaceItemId = ParseInt(fields[9]);
                RingItemId = ParseInt(fields[10]);
            }
        }

        public CustomEquipmentCraftProficiencySheet()
            : base(nameof(CustomEquipmentCraftProficiencySheet))
        {
        }
    }
}
