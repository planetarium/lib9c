using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Per-equipment-grade configuration for the equipment potential option feature.
    /// A single row per grade defines how many potential slots that grade grants and the material
    /// cost consumed to grant (roll) the options. Both the slot count and the cost share the same
    /// key (grade), so they are managed together in one sheet.
    /// </summary>
    [Serializable]
    public class EquipmentPotentialGradeSheet : Sheet<int, EquipmentPotentialGradeSheet.Row>
    {
        /// <summary>
        /// A single grade's potential configuration.
        /// Column order: grade, slot_count, cost_material_id, cost_material_count.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Grade;

            /// <summary>The equipment grade this row applies to.</summary>
            public int Grade { get; private set; }

            /// <summary>The number of potential option slots granted at this grade.</summary>
            public int SlotCount { get; private set; }

            /// <summary>The material item id consumed to grant potential options at this grade.</summary>
            public int CostMaterialId { get; private set; }

            /// <summary>The amount of the cost material consumed per grant.</summary>
            public int CostMaterialCount { get; private set; }

            /// <summary>
            /// Parses a CSV row into this configuration row.
            /// </summary>
            /// <param name="fields">The CSV fields in column order.</param>
            public override void Set(IReadOnlyList<string> fields)
            {
                Grade = ParseInt(fields[0]);
                SlotCount = ParseInt(fields[1]);
                CostMaterialId = ParseInt(fields[2], 0);
                CostMaterialCount = ParseInt(fields[3], 0);
            }
        }

        /// <summary>
        /// Creates an empty <see cref="EquipmentPotentialGradeSheet"/>.
        /// </summary>
        public EquipmentPotentialGradeSheet() : base(nameof(EquipmentPotentialGradeSheet))
        {
        }
    }
}
