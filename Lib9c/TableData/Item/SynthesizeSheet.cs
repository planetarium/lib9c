using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Represents a SynthesizeSheet.
    /// </summary>
    [Serializable]
    public class SynthesizeSheet : Sheet<int, SynthesizeSheet.Row>
    {
        public const int SucceedRateMin = 0;
        public const int SucceedRateMax = 10000;

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => GradeId;

            public int GradeId { get; private set; }
            public int RequiredCount { get; private set; }
            public int SucceedRate { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                GradeId = ParseInt(fields[0]);
                RequiredCount = ParseInt(fields[1], 25);
                var succeedRate = ParseInt(fields[2], SucceedRateMin);
                SucceedRate = Math.Min(SucceedRateMax, Math.Max(SucceedRateMin, succeedRate));
            }
        }

        public SynthesizeSheet() : base(nameof(SynthesizeSheet))
        {
        }
    }
}
