using System;
using System.Collections.Generic;
using System.Numerics;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.Item
{
    [Serializable]
    public class UnlockCombinationSlotCostSheet : Sheet<int, UnlockCombinationSlotCostSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => SlotId;
            public int SlotId;
            public int CrystalPrice;
            public int GoldenDustPrice;
            public int RubyDustPrice;
            public BigInteger NcgPrice;

            public override void Set(IReadOnlyList<string> fields)
            {
                SlotId = TryParseInt(fields[0], out var slotId) ? slotId : 0;
                CrystalPrice = TryParseInt(fields[1], out var crystalPrice) ? crystalPrice : 0;
                GoldenDustPrice = TryParseInt(fields[2], out var goldenDustPrice) ? goldenDustPrice : 0;
                RubyDustPrice = TryParseInt(fields[3], out var rubyDustPrice) ? rubyDustPrice : 0;
                NcgPrice = TryParseInt(fields[4], out var ncgPrice) ? ncgPrice : 0;
            }
        }

        public UnlockCombinationSlotCostSheet() : base(nameof(UnlockCombinationSlotCostSheet))
        {
        }
    }
}
