using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types;
using Nekoyume.Action;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Represents the board state for infinite tower, tracking clear counts per floor.
    /// </summary>
    [Serializable]
    public class InfiniteTowerBoardState : IState
    {
        /// <summary>
        /// Dictionary mapping floor ID to the number of players who cleared it.
        /// </summary>
        public Dictionary<int, int> FloorClearCounts { get; private set; }

        /// <summary>
        /// The infinite tower ID this board belongs to.
        /// </summary>
        public int InfiniteTowerId { get; private set; }

        /// <summary>
        /// The block index when this board was last updated.
        /// </summary>
        public long LastUpdatedBlockIndex { get; private set; }

        /// <summary>
        /// Creates a new infinite tower board state.
        /// </summary>
        /// <param name="infiniteTowerId">The infinite tower ID.</param>
        public InfiniteTowerBoardState(int infiniteTowerId)
        {
            InfiniteTowerId = infiniteTowerId;
            FloorClearCounts = new Dictionary<int, int>();
            LastUpdatedBlockIndex = 0;
        }

        /// <summary>
        /// Creates an infinite tower board state from serialized data.
        /// </summary>
        /// <param name="serialized">The serialized data.</param>
        public InfiniteTowerBoardState(List serialized)
        {
            if (serialized.Count >= 3)
            {
                InfiniteTowerId = (Integer)serialized[0];
                LastUpdatedBlockIndex = (Integer)serialized[1];

                var floorClearCountsList = (List)serialized[2];
                FloorClearCounts = new Dictionary<int, int>();

                foreach (var value in floorClearCountsList)
                {
                    var innerList = (List)value;
                    var floorId = (Integer)innerList[0];
                    var clearCount = (Integer)innerList[1];
                    FloorClearCounts[floorId] = clearCount;
                }
            }
        }


        /// <summary>
        /// Records a floor clear for a player.
        /// </summary>
        /// <param name="floorId">The floor ID that was cleared.</param>
        /// <param name="blockIndex">The current block index.</param>
        public void RecordFloorClear(int floorId, long blockIndex)
        {
            if (!FloorClearCounts.ContainsKey(floorId))
            {
                FloorClearCounts[floorId] = 0;
            }

            FloorClearCounts[floorId]++;
            LastUpdatedBlockIndex = blockIndex;
        }

        /// <summary>
        /// Gets the clear count for a specific floor.
        /// </summary>
        /// <param name="floorId">The floor ID.</param>
        /// <returns>The number of players who cleared this floor.</returns>
        public int GetFloorClearCount(int floorId)
        {
            return FloorClearCounts.TryGetValue(floorId, out var count) ? count : 0;
        }

        /// <summary>
        /// Gets the total number of floors that have been cleared by at least one player.
        /// </summary>
        /// <returns>The number of floors with at least one clear.</returns>
        public int GetTotalClearedFloors()
        {
            return FloorClearCounts.Count(kvp => kvp.Value > 0);
        }

        /// <summary>
        /// Gets the top floors by clear count.
        /// </summary>
        /// <param name="limit">The maximum number of floors to return.</param>
        /// <returns>A list of floor IDs sorted by clear count (descending).</returns>
        public List<int> GetTopFloorsByClearCount(int limit = 10)
        {
            return FloorClearCounts
                .Where(kvp => kvp.Value > 0)
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key)
                .Take(limit)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Gets statistics about the board state.
        /// </summary>
        /// <returns>A dictionary containing various statistics.</returns>
        public Dictionary<string, object> GetStatistics()
        {
            var totalClears = FloorClearCounts.Values.Sum();
            var maxClearCount = FloorClearCounts.Values.DefaultIfEmpty(0).Max();
            var averageClearCount = FloorClearCounts.Count > 0 ? (double)totalClears / FloorClearCounts.Count : 0;

            return new Dictionary<string, object>
            {
                ["total_clears"] = totalClears,
                ["total_floors_with_clears"] = GetTotalClearedFloors(),
                ["max_clear_count"] = maxClearCount,
                ["average_clear_count"] = Math.Round(averageClearCount, 2),
                ["last_updated_block_index"] = LastUpdatedBlockIndex
            };
        }

        /// <summary>
        /// Serializes the state to Bencodex format.
        /// </summary>
        /// <returns>The serialized state.</returns>
        public IValue Serialize()
        {
            var floorClearCountsList = new List(FloorClearCounts.Select(kv => List.Empty.Add(kv.Key).Add(kv.Value)));
            return List.Empty
                .Add(InfiniteTowerId)
                .Add(LastUpdatedBlockIndex)
                .Add(floorClearCountsList);
        }
    }
}
