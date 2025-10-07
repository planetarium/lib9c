using System;
using System.Collections.Generic;
using System.Linq;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.Event
{
    /// <summary>
    /// Represents the PatrolRewardSheet, which defines patrol reward policies and rules.
    /// </summary>
    [Serializable]
    public class PatrolRewardSheet : Sheet<int, PatrolRewardSheet.Row>
    {
        /// <summary>
        /// Represents a reward model consisting of item counts, IDs, and tickers.
        /// </summary>
        [Serializable]
        public class RewardModel
        {
            public int Count;
            public int ItemId;
            public string Ticker;

            public RewardModel(int count, int itemId, string ticker)
            {
                Count = count;
                ItemId = itemId;
                Ticker = ticker;
            }
        }

        /// <summary>
        /// Represents a row in the PatrolRewardSheet, defining patrol reward policies for specific levels and intervals.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<int>
        {
            public const int MaxRewardCount = 100;
            public override int Key => Id;
            public int Id { get; set; }
            public long StartedBlockIndex { get; set; }
            public long EndedBlockIndex { get; set; }
            public long Interval { get; set; }
            public int MinimumLevel { get; set; }
            public int? MaxLevel { get; set; }
            public List<RewardModel> Rewards { get; set; } = new ();

            /// <summary>
            /// Sets the row data using a list of string fields.
            /// </summary>
            /// <param name="fields">The fields to parse into the row.</param>
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                StartedBlockIndex = ParseLong(fields[1]);
                EndedBlockIndex = ParseLong(fields[2]);
                Interval = ParseLong(fields[3]);
                MinimumLevel = ParseInt(fields[4]);
                if (string.IsNullOrEmpty(fields[5]))
                {
                    MaxLevel = null;
                }
                else
                {
                    MaxLevel = ParseInt(fields[5]);
                }

                for (var i = 0; i < MaxRewardCount; i++)
                {
                    var idx = 6 + i * 3;
                    if (fields.Count >= idx + 3 &&
                        TryParseInt(fields[idx], out _))
                    {
                        TryParseInt(fields[idx + 1], out var itemId);
                        Rewards.Add(new (ParseInt(fields[idx]), itemId, fields[idx + 2]));
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Finds the reward policy row based on avatar level and block index.
        /// </summary>
        /// <param name="level">The avatar's level.</param>
        /// <param name="blockIndex">The current block index.</param>
        /// <returns>The corresponding reward policy row.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no activated policy matches the criteria.</exception>
        public Row FindByLevel(int level, long blockIndex)
        {
            var orderedRows = Values
                .Where(r => r.StartedBlockIndex <= blockIndex && blockIndex <= r.EndedBlockIndex)
                .OrderByDescending(i => i.MinimumLevel).ToList();

            if (!orderedRows.Any())
            {
                throw new InvalidOperationException("can't find activated policy");
            }

            foreach (var row in orderedRows)
            {
                if (row.MinimumLevel <= level)
                {
                    return row;
                }
            }

            return orderedRows.Last();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PatrolRewardSheet"/> class.
        /// </summary>
        public PatrolRewardSheet() : base(nameof(PatrolRewardSheet))
        {
        }
    }
}
