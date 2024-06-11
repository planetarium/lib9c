using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.AdventureBoss
{
    [Serializable]
    public class AdventureBossNcgRewardRatioSheet : Sheet<int, AdventureBossNcgRewardRatioSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id;
            public int ItemId;
            public decimal Ratio;

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = TryParseInt(fields[0], out var id) ? id : 0;
                ItemId = TryParseInt(fields[1], out var itemId) ? itemId : 0;
                Ratio = TryParseDecimal(fields[2], out var ratio) ? ratio : 0m;
            }
        }

        public AdventureBossNcgRewardRatioSheet(string name) : base(name)
        {
        }
    }
}
