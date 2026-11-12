using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Crystal
{
    /// <summary>
    /// Hammer points and their crystal price per equipment recipe.
    /// </summary>
    public class CrystalHammerPointSheet : Sheet<int, CrystalHammerPointSheet.Row>
    {
        /// <summary>
        /// One recipe's hammer point settings.
        /// </summary>
        public class Row : SheetRow<int>
        {
            /// <summary>
            /// The equipment recipe id.
            /// </summary>
            public int RecipeId { get; private set; }
            /// <summary>
            /// The hammer points needed for a super craft.
            /// </summary>
            public int MaxPoint { get; private set; }
            /// <summary>
            /// Crystal cost of one hammer point (column <c>crystal_cost</c>).
            /// </summary>
            public long CRYSTAL { get; private set; }
            /// <inheritdoc />
            public override int Key => RecipeId;

            public override void Set(IReadOnlyList<string> fields)
            {
                RecipeId = ParseInt(fields[0], 0);
                MaxPoint = ParseInt(fields[1], 0);
                CRYSTAL = ParseLong(fields[2], 0);
            }
        }

        /// <summary>
        /// Creates an empty sheet.
        /// </summary>
        public CrystalHammerPointSheet() : base(nameof(CrystalHammerPointSheet))
        {
        }
    }
}
