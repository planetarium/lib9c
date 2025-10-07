using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Model.Item;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.Item
{
    /// <summary>
    /// Represents a SynthesizeSheet.
    /// </summary>
    [Serializable]
    public class SynthesizeSheet : Sheet<int, SynthesizeSheet.Row>
    {
        /// <summary>
        /// synthesize data for each subtype
        /// </summary>
        public struct SynthesizeData
        {
            public int RequiredCount;
            public int SucceedRate;
        }

        public const int SucceedRateMin = 0;
        public const int SucceedRateMax = 10000;

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
                var succeedRate = ParseInt(fields[3], SucceedRateMin);
                RequiredCountDict = new Dictionary<ItemSubType, SynthesizeData>
                {
                    [itemSubType] = new ()
                    {
                        RequiredCount = requiredCount,
                        SucceedRate = Math.Min(SucceedRateMax, Math.Max(SucceedRateMin, succeedRate)),
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
