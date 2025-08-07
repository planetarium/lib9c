using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.TableData
{
    using static TableExtensions;

    /// <summary>
    /// Represents a sheet containing rune cost data organized by level ranges.
    /// This sheet stores cost information for rune enhancement across different level intervals,
    /// allowing for efficient data compression and retrieval.
    /// </summary>
    [Serializable]
    public class RuneCostSheet : Sheet<int, RuneCostSheet.Row>
    {
        /// <summary>
        /// Represents cost data for a specific level range of rune enhancement.
        /// Contains information about required materials and success rates for a given level interval.
        /// </summary>
        [Serializable]
        public class RuneCostData
        {
            /// <summary>
            /// The starting level of this cost range (inclusive)
            /// </summary>
            public int LevelStart { get; }

            /// <summary>
            /// The ending level of this cost range (inclusive)
            /// </summary>
            public int LevelEnd { get; }

            /// <summary>
            /// The quantity of rune stones required for enhancement
            /// </summary>
            public int RuneStoneQuantity { get; }

            /// <summary>
            /// The quantity of crystals required for enhancement
            /// </summary>
            public int CrystalQuantity { get; }

            /// <summary>
            /// The quantity of NCG (Nine Chronicles Gold) required for enhancement
            /// </summary>
            public int NcgQuantity { get; }

            /// <summary>
            /// The success rate percentage for rune enhancement (in basis points, e.g., 10000 = 100%)
            /// </summary>
            public int LevelUpSuccessRate { get; }

            /// <summary>
            /// Initializes a new instance of the RuneCostData class with specified parameters.
            /// </summary>
            /// <param name="levelStart">The starting level of the cost range</param>
            /// <param name="levelEnd">The ending level of the cost range</param>
            /// <param name="runeStoneQuantity">Required rune stone quantity</param>
            /// <param name="crystalQuantity">Required crystal quantity</param>
            /// <param name="ncgQuantity">Required NCG quantity</param>
            /// <param name="levelUpSuccessRate">Success rate for enhancement</param>
            public RuneCostData(
                int levelStart,
                int levelEnd,
                int runeStoneQuantity,
                int crystalQuantity,
                int ncgQuantity,
                int levelUpSuccessRate)
            {
                LevelStart = levelStart;
                LevelEnd = levelEnd;
                RuneStoneQuantity = runeStoneQuantity;
                CrystalQuantity = crystalQuantity;
                NcgQuantity = ncgQuantity;
                LevelUpSuccessRate = levelUpSuccessRate;
            }

            /// <summary>
            /// Determines whether the specified level falls within this cost range.
            /// </summary>
            /// <param name="level">The level to check</param>
            /// <returns>true if the level is within the range (inclusive); otherwise, false</returns>
            public bool ContainsLevel(int level)
            {
                return level >= LevelStart && level <= LevelEnd;
            }
        }

        /// <summary>
        /// Represents a row in the RuneCostSheet containing cost data for a specific rune.
        /// Each row can contain multiple cost data entries for different level ranges.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<int>
        {
            /// <summary>
            /// Gets the unique identifier for this row, which is the rune ID.
            /// </summary>
            public override int Key => RuneId;

            /// <summary>
            /// Gets the rune identifier associated with this cost data.
            /// </summary>
            public int RuneId { get; private set; }

            /// <summary>
            /// Gets the collection of cost data entries for different level ranges.
            /// </summary>
            public List<RuneCostData> Cost { get; private set; }

            /// <summary>
            /// Sets the row data from the provided CSV fields.
            /// </summary>
            /// <param name="fields">The CSV fields containing rune cost information</param>
            public override void Set(IReadOnlyList<string> fields)
            {
                RuneId = ParseInt(fields[0]);
                var levelStart = ParseInt(fields[1]);
                var levelEnd = ParseInt(fields[2]);
                var runeStoneQuantity = ParseInt(fields[3]);
                var crystal = ParseInt(fields[4]);
                var ncg = ParseInt(fields[5]);
                var successRate = ParseInt(fields[6]);
                Cost = new List<RuneCostData>
                {
                   new RuneCostData(levelStart, levelEnd, runeStoneQuantity, crystal, ncg, successRate)
                };
            }

            /// <summary>
            /// Attempts to find cost data for a specific level.
            /// </summary>
            /// <param name="level">The level to search for</param>
            /// <param name="costData">When this method returns, contains the cost data for the specified level,
            /// if found; otherwise, null</param>
            /// <returns>true if cost data was found for the specified level; otherwise, false</returns>
            public bool TryGetCost(int level, out RuneCostData costData)
            {
                if (Cost == null)
                {
                    costData = null;
                    return false;
                }

                costData = Cost.FirstOrDefault(x => x.ContainsLevel(level));
                return costData is not null;
            }

            /// <summary>
            /// Retrieves all cost data entries that overlap with the specified level range.
            /// </summary>
            /// <param name="startLevel">The starting level of the range (inclusive)</param>
            /// <param name="endLevel">The ending level of the range (inclusive)</param>
            /// <returns>An enumerable collection of cost data entries that overlap with the specified range</returns>
            public IEnumerable<RuneCostData> GetCostsInRange(int startLevel, int endLevel)
            {
                return Cost.Where(x =>
                    (x.LevelStart <= endLevel && x.LevelEnd >= startLevel));
            }
        }

        /// <summary>
        /// Initializes a new instance of the RuneCostSheet class.
        /// </summary>
        public RuneCostSheet() : base(nameof(RuneCostSheet))
        {
        }

        /// <summary>
        /// Adds a row to the sheet, handling cases where the key already exists.
        /// If a row with the same key exists, the cost data is merged.
        /// </summary>
        /// <param name="key">The unique identifier for the row</param>
        /// <param name="value">The row data to add</param>
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
