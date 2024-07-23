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
            public override int Key => Proficiency;

            public int Proficiency { get; private set; }

            public BigInteger GoldAmount { get; private set; }
            public int Material1Id { get; private set; }
            public int Material1Amount { get; private set; }
            public int Material2Id { get; private set; }
            public int Material2Amount { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Proficiency = ParseInt(fields[0]);
                GoldAmount = ParseBigInteger(fields[1]);
                Material1Id = ParseInt(fields[2]);
                Material1Amount = ParseInt(fields[3]);
                Material2Id = ParseInt(fields[4]);
                Material2Amount = ParseInt(fields[5]);
            }
        }

        public CustomEquipmentCraftCostSheet() : base(nameof(CustomEquipmentCraftCostSheet))
        {
        }
    }
}
