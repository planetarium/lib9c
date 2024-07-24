using System;
using System.Collections.Generic;
using System.Numerics;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.CustomEquipmentCraft
{
    [Serializable]
    public class CustomEquipmentCraftCostSheet : Sheet<int, CustomEquipmentCraftCostSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public struct MaterialCost
            {
                public int ItemId;
                public int Amount;
            }

            public override int Key => Relationship;

            public int Relationship { get; private set; }

            public BigInteger GoldAmount { get; private set; }
            public List<MaterialCost> MaterialCosts { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Relationship = ParseInt(fields[0]);
                GoldAmount = BigInteger.TryParse(fields[1], out var ga) ? ga : 0;
                MaterialCosts = new List<MaterialCost>();

                var inc = 2;
                for (var i = 0; i < 2; i++)
                {
                    if (TryParseInt(fields[2 + i * inc], out var val))
                    {
                        MaterialCosts.Add(
                            new MaterialCost
                            {
                                ItemId = ParseInt(fields[2 + i * inc]),
                                Amount = ParseInt(fields[3 + i * inc])
                            }
                        );
                    }
                }
            }
        }

        public CustomEquipmentCraftCostSheet() : base(nameof(CustomEquipmentCraftCostSheet))
        {
        }
    }
}
