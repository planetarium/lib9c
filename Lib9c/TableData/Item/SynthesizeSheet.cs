using System;
using System.Collections.Generic;
using Nekoyume.Model.Item;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    using System.Linq;

    [Serializable]
    public class SynthesizeSheet : Sheet<int, SynthesizeSheet.Row>
    {
        public struct SynthesizeData
        {
            public int RequiredCount;
            public float SucceedRate;
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => GradeId;

            public int GradeId { get; private set; }
            public Dictionary<ItemSubType, SynthesizeData> RequiredCountDict { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                GradeId = ParseInt(fields[0]);
                var itemSubType = (ItemSubType) Enum.Parse(typeof(ItemSubType), fields[1]);
                var requiredCount = ParseInt(fields[2], 25);
                var succeedRate = ParseFloat(fields[3], 0.0f);
                RequiredCountDict = new Dictionary<ItemSubType, SynthesizeData>
                {
                    [itemSubType] = new ()
                    {
                        RequiredCount = requiredCount,
                        SucceedRate = succeedRate,
                    },
                };
            }
        }

        public SynthesizeSheet() : base(nameof(SynthesizeSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (!value.RequiredCountDict.Any())
            {
                return;
            }

            row.RequiredCountDict.TryAdd(value.RequiredCountDict.First().Key, value.RequiredCountDict.First().Value);
        }
    }
}
