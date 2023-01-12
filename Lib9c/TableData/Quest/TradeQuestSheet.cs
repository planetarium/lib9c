using System;
using System.Collections.Generic;
using Lib9c.Model.EnumType;

namespace Lib9c.TableData.Quest
{
    [Serializable]
    public class TradeQuestSheet : Sheet<int, TradeQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
            public TradeType Type { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                Type = (TradeType) Enum.Parse(typeof(TradeType), fields[3]);
            }
        }

        public TradeQuestSheet() : base(nameof(TradeQuestSheet))
        {
        }
    }
}
