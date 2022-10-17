using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.EnumType;

namespace Nekoyume.TableData
{
    using static TableExtensions;

    [Serializable]
    public class RuneCostSheet : Sheet<int, RuneCostSheet.Row>
    {
        [Serializable]
        public class RuneCostData
        {
            public int Level;
            public int RuneStoneId;
            public int RuneStoneQuantity;
            public int CrystalQuantity;
            public int NcgQuantity;
            public int LevelUpSuccessRate;

            public RuneCostData(
                int level,
                int runeStoneId,
                int runeStoneQuantity,
                int crystalQuantity,
                int ncgQuantity,
                int levelUpSuccessRate)
            {
                Level = level;
                RuneStoneId = runeStoneId;
                RuneStoneQuantity = runeStoneQuantity;
                CrystalQuantity = crystalQuantity;
                NcgQuantity = ncgQuantity;
                LevelUpSuccessRate = levelUpSuccessRate;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => RuneId;

            public int RuneId { get; private set; }

            public List<RuneCostData> Cost { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                RuneId = ParseInt(fields[0]);
                var level = ParseInt(fields[1]);
                var runeStoneId = ParseInt(fields[2]);
                var runeStoneQuantity = ParseInt(fields[3]);
                var crystal = ParseInt(fields[4]);
                var ncg = ParseInt(fields[5]);
                var successRate = ParseInt(fields[6]);
                Cost = new List<RuneCostData>
                {
                   new RuneCostData(level, runeStoneId, runeStoneQuantity, crystal, ncg, successRate)
                };
            }

            public bool TryGetCost(int level, out RuneCostData costData)
            {
                costData = Cost.FirstOrDefault(x => x.Level.Equals(level));
                return !(costData is null);
            }
        }

        public RuneCostSheet() : base(nameof(RuneCostSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (!value.Cost.Any())
            {
                return;
            }

            row.Cost.Add(value.Cost[0]);
        }
    }
}
