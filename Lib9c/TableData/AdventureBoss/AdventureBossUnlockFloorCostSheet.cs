using System;
using System.Collections.Generic;
using System.Numerics;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.AdventureBoss
{
    [Serializable]
    public class
        AdventureBossUnlockFloorCostSheet : Sheet<int, AdventureBossUnlockFloorCostSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => FloorId;
            public int FloorId;
            public int GoldenDustPrice;
            public BigInteger NcgPrice;

            public override void Set(IReadOnlyList<string> fields)
            {
                FloorId = TryParseInt(fields[0], out var floorId) ? floorId : 0;
                GoldenDustPrice = TryParseInt(fields[1], out var dustPrice) ? dustPrice : 0;
                NcgPrice = TryParseInt(fields[2], out var ncgPrice) ? ncgPrice : 0;
            }
        }

        public AdventureBossUnlockFloorCostSheet() : base(nameof(AdventureBossUnlockFloorCostSheet))
        {
        }
    }
}
