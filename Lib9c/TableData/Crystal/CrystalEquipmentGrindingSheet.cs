using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Crystal
{
    /// <summary>
    /// Crystal and materials paid for grinding each equipment.
    /// </summary>
    public class CrystalEquipmentGrindingSheet: Sheet<int, CrystalEquipmentGrindingSheet.Row>
    {
        /// <summary>
        /// One equipment's grinding reward.
        /// </summary>
        public class Row : SheetRow<int>
        {
            /// <inheritdoc />
            public override int Key => ItemId;
            /// <summary>
            /// The equipment id this row applies to.
            /// </summary>
            public int ItemId;
            /// <summary>
            /// The row whose crystal is added per enhancement level.
            /// </summary>
            public int EnchantBaseId;
            /// <summary>
            /// Crystal paid for grinding this equipment (column <c>gain_crystal</c>).
            /// </summary>
            public long CRYSTAL;
            /// <summary>
            /// Materials also given for grinding.
            /// </summary>
            public List<(int materialId, int count)> RewardMaterials = new();

            public override void Set(IReadOnlyList<string> fields)
            {
                ItemId = ParseInt(fields[0]);
                EnchantBaseId = ParseInt(fields[1]);
                CRYSTAL = ParseLong(fields[2]);

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

        /// <summary>
        /// Creates an empty sheet.
        /// </summary>
        public CrystalEquipmentGrindingSheet() : base(nameof(CrystalEquipmentGrindingSheet))
        {
        }
    }
}
