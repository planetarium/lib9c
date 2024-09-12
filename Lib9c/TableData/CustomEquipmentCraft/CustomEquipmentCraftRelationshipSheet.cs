using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using Nekoyume.Model.Item;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.CustomEquipmentCraft
{
    [Serializable]
    public class
        CustomEquipmentCraftRelationshipSheet
        : Sheet<int, CustomEquipmentCraftRelationshipSheet.Row>
    {
        public struct MaterialCost
        {
            public int ItemId;
            public int Amount;
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Relationship;

            public int Relationship { get; private set; }
            public long CostMultiplier { get; private set; }
            public long RequiredBlockMultiplier { get; private set; }
            public BigInteger GoldAmount { get; private set; }
            public List<MaterialCost> MaterialCosts { get; private set; }
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
                CostMultiplier = ParseLong(fields[1]);
                RequiredBlockMultiplier = ParseLong(fields[2]);
                MinCp = ParseInt(fields[3]);
                MaxCp = ParseInt(fields[4]);
                WeaponItemId = ParseInt(fields[5]);
                ArmorItemId = ParseInt(fields[6]);
                BeltItemId = ParseInt(fields[7]);
                NecklaceItemId = ParseInt(fields[8]);
                RingItemId = ParseInt(fields[9]);

                GoldAmount = BigInteger.TryParse(fields[10], out var ga) ? ga : 0;
                MaterialCosts = new List<MaterialCost>();
                var increment = 2;
                for (var i = 0; i < 2; i++)
                {
                    if (TryParseInt(fields[11 + i * increment], out var val))
                    {
                        MaterialCosts.Add(
                            new MaterialCost
                            {
                                ItemId = ParseInt(fields[11 + i * increment]),
                                Amount = ParseInt(fields[12 + i * increment])
                            }
                        );
                    }
                }
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
