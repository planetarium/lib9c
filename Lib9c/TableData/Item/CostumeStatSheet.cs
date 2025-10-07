using System;
using System.Collections.Generic;
using Lib9c.Model.Stat;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.Item
{
    [Serializable]
    public class CostumeStatSheet : Sheet<int, CostumeStatSheet.Row>
    {
        [Serializable]
        public class Row: SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public int CostumeId { get; private set; }
            public StatType StatType { get; private set; }
            public decimal Stat { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                CostumeId = ParseInt(fields[1]);
                StatType = (StatType) Enum.Parse(typeof(StatType), fields[2]);
                Stat = ParseDecimal(fields[3]);
            }
        }

        public CostumeStatSheet() : base(nameof(CostumeStatSheet))
        {
        }
    }
}
