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
            public override int Key => Proficiency;

            public int Proficiency { get; private set; }
            public int MinSubStat { get; private set; }
            public int MaxSubStat { get; private set; }
            public int RequiredLevel { get; private set; }
            public decimal CostMultiplier { get; private set; }
            public BigInteger AdditionalGold { get; private set; }
            public int AdditionalMaterialId { get; private set; }
            public int AdditionalMaterialAmount { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Proficiency = ParseInt(fields[0]);
                MinSubStat = ParseInt(fields[1]);
                MaxSubStat = ParseInt(fields[2]);
                RequiredLevel = ParseInt(fields[3]);
                CostMultiplier = ParseDecimal(fields[4]);
                AdditionalGold = TryParseInt(fields[5], out var addGold) ? addGold : 0;
                AdditionalMaterialId =
                    TryParseInt(fields[6], out var addMaterialId) ? addMaterialId : 0;
                AdditionalMaterialAmount = TryParseInt(fields[7], out var addMaterialAmt)
                    ? addMaterialAmt
                    : 0;
            }
        }

        public CustomEquipmentCraftProficiencySheet()
            : base(nameof(CustomEquipmentCraftProficiencySheet))
        {
        }
    }
}
