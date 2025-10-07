using System;
using System.Collections.Generic;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.AdventureBoss
{
    [Serializable]
    public class AdventureBossNcgRewardRatioSheet : Sheet<int, AdventureBossNcgRewardRatioSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => ItemId;

            public int ItemId;
            public decimal Ratio;

            public override void Set(IReadOnlyList<string> fields)
            {
                ItemId = TryParseInt(fields[0], out var itemId) ? itemId : 0;
                Ratio = TryParseDecimal(fields[1], out var ratio) ? ratio : 0m;
            }
        }

        public AdventureBossNcgRewardRatioSheet() : base(nameof(AdventureBossNcgRewardRatioSheet))
        {
        }
    }
}
