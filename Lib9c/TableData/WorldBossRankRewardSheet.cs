using System.Collections.Generic;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData
{
    public class WorldBossRankRewardSheet: Sheet<int, WorldBossRankRewardSheet.Row>, IWorldBossRewardSheet
    {
        public class Row : SheetRow<int>, IWorldBossRewardRow
        {
            public override int Key => Id;
            public int Id;
            public int BossId { get; private set; }
            public int Rank { get; private set; }
            public int Rune { get; private set; }
            public int Crystal { get; private set; }
            public int Circle { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                BossId = ParseInt(fields[1]);
                Rank = ParseInt(fields[2]);
                Rune = ParseInt(fields[3]);
                Crystal = ParseInt(fields[4]);
                if (fields.Count > 5)
                {
                    Circle = ParseInt(fields[5]);
                }
            }
        }

        public WorldBossRankRewardSheet() : base(nameof(WorldBossRankRewardSheet))
        {
        }

        public IReadOnlyList<IWorldBossRewardRow> OrderedRows => OrderedList;
    }
}
