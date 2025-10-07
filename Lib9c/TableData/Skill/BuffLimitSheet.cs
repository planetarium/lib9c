using System;
using System.Collections.Generic;
using Lib9c.Model.Stat;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.Skill
{
    using OperationType = StatModifier.OperationType;

    public class BuffLimitSheet : Sheet<int, BuffLimitSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => GroupId;

            public int GroupId { get; set; }

            public StatModifier.OperationType StatModifier { get; private set; }

            public int Value { get; set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                GroupId = ParseInt(fields[0]);
                StatModifier = (OperationType)Enum.Parse(typeof(OperationType), fields[1]);
                Value = ParseInt(fields[2]);
            }

            public StatModifier GetModifier(StatType statType)
            {
                return new StatModifier(statType, StatModifier, Value);
            }
        }

        public BuffLimitSheet() : base(nameof(BuffLimitSheet))
        {
        }
    }
}
