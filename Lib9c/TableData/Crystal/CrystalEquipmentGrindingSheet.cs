using System.Collections.Generic;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.Crystal
{
    public class CrystalEquipmentGrindingSheet: Sheet<int, CrystalEquipmentGrindingSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => ItemId;
            public int ItemId;
            public int EnchantBaseId;
            public int CRYSTAL;
            public List<(int materialId, int count)> RewardMaterials = new();

            public override void Set(IReadOnlyList<string> fields)
            {
                ItemId = ParseInt(fields[0]);
                EnchantBaseId = ParseInt(fields[1]);
                CRYSTAL = ParseInt(fields[2]);

                if (fields.Count > 3)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var offset = i * 2;
                        if (!TryParseInt(fields[3 + offset], out var materialId) || materialId == 0)
                        {
                            continue;
                        }

                        RewardMaterials.Add((materialId, ParseInt(fields[4 + offset])));
                    }
                }
            }
        }

        public CrystalEquipmentGrindingSheet() : base(nameof(CrystalEquipmentGrindingSheet))
        {
        }
    }
}
