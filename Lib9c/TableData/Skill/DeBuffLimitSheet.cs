using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using Org.BouncyCastle.Tls;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class DeBuffLimitSheet : Sheet<int, DeBuffLimitSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => GroupId;

            public int GroupId { get; set; }

            public int Value { get; set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                GroupId = ParseInt(fields[0]);
                Value = ParseInt(fields[1]);
            }

            public StatModifier GetModifier(StatType statType)
            {
                return new StatModifier(statType, StatModifier.OperationType.Percentage, Value);
            }
        }

        public DeBuffLimitSheet() : base(nameof(DeBuffLimitSheet))
        {
        }
    }
}
