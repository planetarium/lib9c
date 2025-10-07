using System;
using System.Collections.Generic;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.AdventureBoss
{
    [Serializable]
    public class AdventureBossFloorPointSheet : Sheet<int, AdventureBossFloorPointSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => FloorId;

            public int FloorId;
            public int MinPoint;
            public int MaxPoint;

            public override void Set(IReadOnlyList<string> fields)
            {
                FloorId = TryParseInt(fields[0], out var floorId) ? floorId : 0;
                MinPoint = TryParseInt(fields[1], out var minPoint) ? minPoint : 0;
                MaxPoint = TryParseInt(fields[2], out var maxPoint) ? maxPoint : 0;
            }
        }

        public AdventureBossFloorPointSheet() : base(nameof(AdventureBossFloorPointSheet))
        {
        }
    }
}
