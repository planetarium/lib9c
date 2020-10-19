using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    public class ArenaConfig
    {
        public int ArenaScoreDefault { get; private set; }
        public int ArenaChallengeCountMax { get; private set; } 
        public int DailyArenaInterval { get; private set; }
        public int WeeklyArenaInterval { get; private set; }

        public ArenaConfig()
        {
        }

        public ArenaConfig(string csv)
        {
            var sheet = new ArenaConfigSheet();
            sheet.Set(csv);
            foreach (var row in sheet.Values)
            {
                Update(row);
            }
        }

        public ArenaConfig(ArenaConfigSheet sheet)
        {
            foreach (var row in sheet.Values)
            {
                Update(row);
            }
        }

        public void Set(ArenaConfigSheet sheet)
        {
            foreach (var row in sheet)
            {
                Update(row);
            }
        }

        public void Update(ArenaConfigSheet.Row row)
        {
            switch (row.Key)
            {
                case "arena_score_default":
                    ArenaScoreDefault = TableExtensions.ParseInt(row.Value);
                    break;
                case "arena_challenge_count_max":
                    ArenaChallengeCountMax = TableExtensions.ParseInt(row.Value);
                    break;
                case "daily_arena_interval":
                    DailyArenaInterval = TableExtensions.ParseInt(row.Value);
                    break;
                case "weekly_arena_interval":
                    WeeklyArenaInterval = TableExtensions.ParseInt(row.Value);
                    break;
            }
        }
    }
}
