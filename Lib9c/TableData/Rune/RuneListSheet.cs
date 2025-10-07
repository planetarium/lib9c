using System.Collections.Generic;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.Rune
{
    public class RuneListSheet : Sheet<int, RuneListSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public int Id;
            public int Grade;
            public int RuneType;
            public int RequiredLevel;
            public int UsePlace;
            public int BonusCoef;

            public override int Key => Id;
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                Grade = ParseInt(fields[1]);
                RuneType = ParseInt(fields[2]);
                RequiredLevel = ParseInt(fields[3]);
                UsePlace = ParseInt(fields[4]);
                BonusCoef = ParseInt(fields[5]);
            }
        }

        public RuneListSheet() : base(nameof(RuneListSheet))
        {
        }
    }
}
