using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class CostumeStatSheet : Sheet<int, CostumeStatSheet.Row>
    {
        [Serializable]
        public class Row: SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public StatType StatType { get; private set; }
            public int Stat { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                StatType = (StatType) Enum.Parse(typeof(StatType), fields[1]);
                Stat = ParseInt(fields[2]);
            }
        }

        public CostumeStatSheet() : base(nameof(CostumeStatSheet))
        {
        }
    }
}
