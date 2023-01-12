using System.Collections.Generic;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData
{
    public class RuneSheet : Sheet<int, RuneSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public int Id;
            public string Ticker;
            public override int Key => Id;
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                Ticker = fields[1];
            }
        }

        public RuneSheet() : base(nameof(RuneSheet))
        {
        }
    }
}
