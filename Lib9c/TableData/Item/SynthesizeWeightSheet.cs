using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    using System.Linq;

    [Serializable]
    public class SynthesizeWeightSheet : Sheet<int, SynthesizeWeightSheet.Row>
    {
        public const int DefaultWeight = 10000;

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => GradeId;

            public int GradeId { get; private set; }

            public Dictionary<int, int> WeightDict { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                GradeId = ParseInt(fields[0]);

                WeightDict = new Dictionary<int, int>();
                var itemId = ParseInt(fields[1]);
                var weight = ParseInt(fields[2], DefaultWeight);
                WeightDict.Add(itemId, weight);
            }
        }

        public SynthesizeWeightSheet() : base(nameof(SynthesizeWeightSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (!value.WeightDict.Any())
            {
                return;
            }

            row.WeightDict.TryAdd(value.WeightDict.First().Key, value.WeightDict.First().Value);
        }
    }
}
