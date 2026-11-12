using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Crystal
{
    [Serializable]
    /// <summary>
    /// Crystal prices of materials that can be paid with crystal instead.
    /// </summary>
    public class CrystalMaterialCostSheet: Sheet<int, CrystalMaterialCostSheet.Row>
    {
        /// <summary>
        /// The crystal price of one material.
        /// </summary>
        public class Row : SheetRow<int>
        {
            /// <summary>
            /// The material id.
            /// </summary>
            public int ItemId { get; private set; }
            /// <summary>
            /// Crystal that replaces one of the material.
            /// </summary>
            public long CRYSTAL { get; private set; }
            /// <inheritdoc />
            public override int Key => ItemId;

            public override void Set(IReadOnlyList<string> fields)
            {
                ItemId = ParseInt(fields[0]);
                CRYSTAL = ParseLong(fields[1]);
            }
        }

        /// <summary>
        /// Creates an empty sheet.
        /// </summary>
        public CrystalMaterialCostSheet() : base(nameof(CrystalMaterialCostSheet))
        {
        }
    }
}
