using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    using System.Linq;

    /// <summary>
    /// Represents a SynthesizeWeightSheet.
    /// </summary>
    [Serializable]
    public class SynthesizeWeightSheet : Sheet<int, SynthesizeWeightSheet.Row>
    {
        public const int DefaultWeight = 10000;

        /// <summary>
        /// Key of the row that switches the sheet from "unlisted means <see cref="DefaultWeight"/>"
        /// to "unlisted means 0". No item carries id 0, so the row is a flag and never a weight.
        /// </summary>
        public const int StrictModeKey = 0;

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => ItemId;

            public int ItemId { get; private set; }
            public int Weight { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                ItemId = ParseInt(fields[0]);
                Weight = TryParseInt(fields[1], out var weight) ? weight : DefaultWeight;
            }
        }

        /// <summary>
        /// Whether an item the sheet does not list is excluded from synthesis results instead of
        /// being drawn with <see cref="DefaultWeight"/>.
        /// </summary>
        /// <remarks>
        /// The default is "included", so a new item enters every result pool of its grade and sub
        /// type the moment it is added to an item sheet, and stays there until someone remembers
        /// to write a 0 here. That is how four grade 8 costumes were handed out in 2026-08.
        /// Flipping the default is a consensus change, so it is switched on per chain by patching
        /// <see cref="StrictModeKey"/> into the sheet rather than by a release: a chain that has
        /// not been patched replays exactly as it did before, and the switch is undone by deleting
        /// the row.
        /// </remarks>
        public bool IsStrict => ContainsKey(StrictModeKey);

        /// <summary>
        /// Creates an empty sheet.
        /// </summary>
        public SynthesizeWeightSheet() : base(nameof(SynthesizeWeightSheet))
        {
        }
    }
}
