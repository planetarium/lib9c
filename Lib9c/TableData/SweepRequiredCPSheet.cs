using System.Collections.Generic;

namespace Nekoyume.TableData
{
    using static TableExtensions;

    public class SweepRequiredCPSheet : Sheet<int, SweepRequiredCPSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => StageId;
            public int StageId { get; private set; }
            public int RequiredCP { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                StageId = ParseInt(fields[0]);
                RequiredCP = ParseInt(fields[1]);
            }

            public Row CloneWithStageId(int newStageId)
            {
                return new Row
                {
                    StageId = newStageId,
                    RequiredCP = RequiredCP,
                };
            }
        }

        public SweepRequiredCPSheet() : base(nameof(SweepRequiredCPSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            base.AddRow(key, value);

            // Extend hard stages as a continuation of normal stages:
            // stage 451..900 duplicates stage 1..450.
            if (key >= 1 && key <= 450)
            {
                var extendedKey = key + 450;
                if (!ContainsKey(extendedKey))
                {
                    base.AddRow(extendedKey, value.CloneWithStageId(extendedKey));
                }
            }
        }
    }
}
