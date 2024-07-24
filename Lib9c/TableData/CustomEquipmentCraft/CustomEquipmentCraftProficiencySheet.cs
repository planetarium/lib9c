using System;
using System.Collections.Generic;
using System.ComponentModel;
using Nekoyume.Model.Item;
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
            public override int Key => Proficiency;

            public int Proficiency { get; private set; }
            public decimal CostMultiplier { get; private set; }
            public int MinCp { get; private set; }
            public int MaxCp { get; private set; }
            public int WeaponItemId { get; private set; }
            public int ArmorItemId { get; private set; }
            public int BeltItemId { get; private set; }
            public int NecklaceItemId { get; private set; }
            public int RingItemId { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Proficiency = ParseInt(fields[0]);
                CostMultiplier = ParseDecimal(fields[1]);
                MinCp = ParseInt(fields[2]);
                MaxCp = ParseInt(fields[3]);
                WeaponItemId = ParseInt(fields[4]);
                ArmorItemId = ParseInt(fields[5]);
                BeltItemId = ParseInt(fields[6]);
                NecklaceItemId = ParseInt(fields[7]);
                RingItemId = ParseInt(fields[8]);
            }

            public int GetItemId(ItemSubType itemSubType)
            {
                return itemSubType switch
                {
                    ItemSubType.Weapon => WeaponItemId,
                    ItemSubType.Armor => ArmorItemId,
                    ItemSubType.Belt => BeltItemId,
                    ItemSubType.Necklace => NecklaceItemId,
                    ItemSubType.Ring => RingItemId,
                    _ => throw new InvalidEnumArgumentException(
                        $"{itemSubType} it not valid ItemSubType.")
                };
            }
        }

        public CustomEquipmentCraftProficiencySheet()
            : base(nameof(CustomEquipmentCraftProficiencySheet))
        {
        }
    }
}
