using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class ArenaConfigState : State
    {
        public static readonly Address Address = Addresses.ArenaConfig;
        public int ArenaScoreDefault { get; private set; }
        public int ArenaChallengeCountMax { get; private set; }
        public int DailyArenaInterval { get; private set; }
        public int WeeklyArenaInterval { get; private set; }

        public ArenaConfigState() : base(Address)
        {
        }

        public ArenaConfigState(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "arena_score_default", out var value))
            {
                ArenaScoreDefault = value.ToInteger();
            }
            if (serialized.TryGetValue((Text) "arena_challenge_count_max", out var value2))
            {
                ArenaChallengeCountMax = value2.ToInteger();
            }
            if (serialized.TryGetValue((Text) "daily_arena_interval", out var value3))
            {
                DailyArenaInterval = value3.ToInteger();
            }
            if (serialized.TryGetValue((Text) "weekly_arena_interval", out var value4))
            {
                WeeklyArenaInterval = value4.ToInteger();
            }
        }

        public ArenaConfigState(string csv) : base(Address)
        {
            var sheet = new ArenaConfigSheet();
            sheet.Set(csv);
            foreach (var row in sheet.Values)
            {
                Update(row);
            }
        }

        public ArenaConfigState(ArenaConfigSheet sheet) : base(Address)
        {
            foreach (var row in sheet.Values)
            {
                Update(row);
            }
        }

        public override IValue Serialize()
        {
            var values = new Dictionary<IKey, IValue>
            {
                [(Text) "arena_score_default"] = ArenaScoreDefault.Serialize(),
                [(Text) "arena_challenge_count_max"] = ArenaChallengeCountMax.Serialize(),
                [(Text) "daily_arena_interval"] = DailyArenaInterval.Serialize(),
                [(Text) "weekly_arena_interval"] = WeeklyArenaInterval.Serialize(),
            };
            return new Dictionary(values.Union((Dictionary) base.Serialize()));
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
