using System.Collections.Generic;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.Rune
{
    public class RuneLevelBonusSheet : Sheet<int, RuneLevelBonusSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public int Id;
            public int RuneLevel;
            public int Bonus;

            public override int Key => Id;

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                RuneLevel = ParseInt(fields[1]);
                Bonus = ParseInt(fields[2]);
            }
        }

        public RuneLevelBonusSheet() : base(nameof(RuneLevelBonusSheet))
        {
        }
    }
}
