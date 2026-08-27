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
        /// <summary>
        /// Weight of an item this sheet does not list, or lists with an unreadable weight.
        /// </summary>
        /// <remarks>
        /// This used to be 10000, which meant a result pool held every item of its grade and sub
        /// type until someone remembered to write a 0 here. An item sheet row was therefore enough
        /// to put a costume into circulation, which is how four grade 8 costumes were handed out
        /// in 2026-08. A pool is spelled out now instead: what this sheet does not list is not
        /// drawn.
        /// </remarks>
        public const int DefaultWeight = 0;

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
        /// Creates an empty sheet.
        /// </summary>
        public SynthesizeWeightSheet() : base(nameof(SynthesizeWeightSheet))
        {
        }
    }
}
