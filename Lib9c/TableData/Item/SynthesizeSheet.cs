using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class SynthesizeSheet : Sheet<int, SynthesizeSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => GradeId;

            public int GradeId { get; private set; }
            public int RequiredCount { get; private set; }
            public float SucceedRate { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                GradeId = ParseInt(fields[0]);
                RequiredCount = ParseInt(fields[1], 25);
                SucceedRate = ParseFloat(fields[2], 0.0f);
            }
        }

        public SynthesizeSheet() : base(nameof(SynthesizeSheet))
        {
        }
    }
}
