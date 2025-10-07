using System;
using Lib9c.TableData.Item;

namespace Lib9c.TableData.Event
{
    [Serializable]
    public class EventConsumableItemRecipeSheet
        : Sheet<int, EventConsumableItemRecipeSheet.Row>
    {
        [Serializable]
        public class Row : ConsumableItemRecipeSheet.Row
        {
        }

        public EventConsumableItemRecipeSheet()
            : base(nameof(EventConsumableItemRecipeSheet))
        {
        }
    }
}
