using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using Lib9c.Model.Item;
using Libplanet.Action;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.CustomEquipmentCraft
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

        // Total CP range is divided to several groups with group ratio. (Max 10 groups)
        public struct CpGroup
        {
            public int Ratio;
            public int MinCp;
            public int MaxCp;

            public int SelectCp(IRandom random)
            {
                return random.Next(MinCp, MaxCp + 1);
            }
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

            public List<CpGroup> CpGroups { get; private set; }
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
                CpGroups = new List<CpGroup>();
                const int groupCount = 10;
                var increment = 3;
                for (var i = 0; i < groupCount; i++)
                {
                    var col = 3 + i * increment;
                    if (TryParseInt(fields[col], out var min))
                    {
                        CpGroups.Add(new CpGroup
                        {
                            MinCp = min,
                            MaxCp = ParseInt(fields[col + 1]),
                            Ratio = ParseInt(fields[col + 2]),
                        });
                    }
                }

                WeaponItemId = ParseInt(fields[33]);
                ArmorItemId = ParseInt(fields[34]);
                BeltItemId = ParseInt(fields[35]);
                NecklaceItemId = ParseInt(fields[36]);
                RingItemId = ParseInt(fields[37]);

                GoldAmount = BigInteger.TryParse(fields[38], out var ga) ? ga : 0;
                MaterialCosts = new List<MaterialCost>();
                const int materialCount = 2;
                increment = 2;
                for (var i = 0; i < materialCount; i++)
                {
                    if (TryParseInt(fields[39 + i * increment], out var val))
                    {
                        MaterialCosts.Add(
                            new MaterialCost
                            {
                                ItemId = ParseInt(fields[39 + i * increment]),
                                Amount = ParseInt(fields[40 + i * increment])
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
