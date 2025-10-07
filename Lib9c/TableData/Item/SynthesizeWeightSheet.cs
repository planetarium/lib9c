using System;
using System.Collections.Generic;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.Item
{
    /// <summary>
    /// Represents a SynthesizeWeightSheet.
    /// </summary>
    [Serializable]
    public class SynthesizeWeightSheet : Sheet<int, SynthesizeWeightSheet.Row>
    {
        public const int DefaultWeight = 10000;

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

        public SynthesizeWeightSheet() : base(nameof(SynthesizeWeightSheet))
        {
        }
    }
}
