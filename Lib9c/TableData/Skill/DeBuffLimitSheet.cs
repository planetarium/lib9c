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
            public override int Key => Id;

            public int Id { get; set; }
            public StatModifier Modifier { get; set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                Modifier = new StatModifier(
                    (StatType) Enum.Parse(typeof(StatType), fields[1]),
                    StatModifier.OperationType.Percentage,
                    ParseInt(fields[2])
                );
            }
        }

        public DeBuffLimitSheet() : base(nameof(DeBuffLimitSheet))
        {
        }
    }
}
