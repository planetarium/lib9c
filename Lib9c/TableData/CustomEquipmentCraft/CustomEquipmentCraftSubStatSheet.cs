using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.CustomEquipmentCraft
{
    [Serializable]
    public class CustomEquipmentCraftSubStatSheet : Sheet<int, CustomEquipmentCraftSubStatSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public int HpRatio { get; private set; }
            public int AtkRatio { get; private set; }
            public int DefRatio { get; private set; }
            public int CriRatio { get; private set; }
            public int HitRatio { get; private set; }
            public int SpdRatio { get; private set; }
            public int DrvRatio { get; private set; }
            public int DrrRatio { get; private set; }
            public int CdmgRatio { get; private set; }
            public int ApRatio { get; private set; }
            public int ThornRatio { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                HpRatio = TryParseInt(fields[1], out var ratio) ? ratio : 0;
                AtkRatio = TryParseInt(fields[2], out ratio) ? ratio : 0;
                DefRatio = TryParseInt(fields[3], out ratio) ? ratio : 0;
                CriRatio = TryParseInt(fields[4], out ratio) ? ratio : 0;
                HitRatio = TryParseInt(fields[5], out ratio) ? ratio : 0;
                SpdRatio = TryParseInt(fields[6], out ratio) ? ratio : 0;
                DrvRatio = TryParseInt(fields[7], out ratio) ? ratio : 0;
                DrrRatio = TryParseInt(fields[8], out ratio) ? ratio : 0;
                CdmgRatio = TryParseInt(fields[9], out ratio) ? ratio : 0;
                ApRatio = TryParseInt(fields[10], out ratio) ? ratio : 0;
                ThornRatio = TryParseInt(fields[11], out ratio) ? ratio : 0;
            }
        }

        public CustomEquipmentCraftSubStatSheet() : base(nameof(CustomEquipmentCraftSubStatSheet))
        {
        }
    }
}
