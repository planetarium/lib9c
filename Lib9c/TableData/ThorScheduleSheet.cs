using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.TableData
{
    using static TableExtensions;

    [Serializable]
    public class ThorScheduleSheet : Sheet<int, ThorScheduleSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }

            public long StartBlockIndex { get; private set; }

            public long EndBlockIndex { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                StartBlockIndex = ParseLong(fields[1]);
                EndBlockIndex = ParseLong(fields[2]);
            }

            public bool IsOpened(long blockIndex)
            {
                return StartBlockIndex <= blockIndex && blockIndex <= EndBlockIndex;
            }
        }

        public ThorScheduleSheet() : base(nameof(ThorScheduleSheet))
        {
        }

        public Row GetRowByBlockIndex(long blockIndex) => OrderedList.FirstOrDefault(row =>
            row.IsOpened(blockIndex));
    }
}
