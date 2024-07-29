using System;
using System.Collections.Generic;
using System.ComponentModel;
using Nekoyume.Model.Item;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.CustomEquipmentCraft
{
    [Serializable]
    public class
        CustomEquipmentCraftRelationshipSheet
        : Sheet<int, CustomEquipmentCraftRelationshipSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Relationship;

            public int Relationship { get; private set; }
            public decimal CostMultiplier { get; private set; }
            public decimal RequiredBlockMultiplier { get; private set; }
            public int MinCp { get; private set; }
            public int MaxCp { get; private set; }
            public int WeaponItemId { get; private set; }
            public int ArmorItemId { get; private set; }
            public int BeltItemId { get; private set; }
            public int NecklaceItemId { get; private set; }
            public int RingItemId { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Relationship = ParseInt(fields[0]);
                CostMultiplier = ParseDecimal(fields[1]);
                RequiredBlockMultiplier = ParseDecimal(fields[2]);
                MinCp = ParseInt(fields[3]);
                MaxCp = ParseInt(fields[4]);
                WeaponItemId = ParseInt(fields[5]);
                ArmorItemId = ParseInt(fields[6]);
                BeltItemId = ParseInt(fields[7]);
                NecklaceItemId = ParseInt(fields[8]);
                RingItemId = ParseInt(fields[9]);
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

        public CustomEquipmentCraftRelationshipSheet()
            : base(nameof(CustomEquipmentCraftRelationshipSheet))
        {
        }
    }
}
