using System;
using Lib9c.TableData.WorldAndStage;

namespace Lib9c.TableData.Event
{
    [Serializable]
    public class EventDungeonSheet : Sheet<int, EventDungeonSheet.Row>
    {
        [Serializable]
        public class Row : WorldSheet.Row
        {
        }

        public EventDungeonSheet() : base(nameof(EventDungeonSheet))
        {
        }
    }
}
