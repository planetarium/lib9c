using System;
using System.Collections.Generic;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.Quest
{
    [Serializable]
    public class CombinationEquipmentQuestSheet : Sheet<int, CombinationEquipmentQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
            public int RecipeId { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                RecipeId = ParseInt(fields[3]);
            }
        }

        public CombinationEquipmentQuestSheet() : base(nameof(CombinationEquipmentQuestSheet))
        {
        }
    }
}
