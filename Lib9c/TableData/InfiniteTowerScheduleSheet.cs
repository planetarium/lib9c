using System;
using System.Collections.Generic;
using Nekoyume.Action.Exceptions;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Infinite tower schedule sheet for managing season schedules and ticket policies.
    /// </summary>
    [Serializable]
    public class InfiniteTowerScheduleSheet : Sheet<int, InfiniteTowerScheduleSheet.Row>
    {
        public InfiniteTowerScheduleSheet() : base(nameof(InfiniteTowerScheduleSheet)) { }

        /// <summary>
        /// Represents a row in the InfiniteTowerScheduleSheet containing schedule data.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<int>
        {
            /// <summary>
            /// Gets the schedule ID as the key for this row.
            /// </summary>
            public override int Key => Id;

            /// <summary>
            /// Gets the unique identifier for this schedule.
            /// </summary>
            public int Id { get; private set; }

            /// <summary>
            /// Gets the infinite tower ID this schedule belongs to.
            /// </summary>
            public int InfiniteTowerId { get; private set; }

            /// <summary>
            /// Gets the block index when the schedule starts.
            /// </summary>
            public long StartBlockIndex { get; private set; }

            /// <summary>
            /// Gets the block index when the schedule ends.
            /// </summary>
            public long EndBlockIndex { get; private set; }

            /// <summary>
            /// Gets the number of free tickets given daily.
            /// </summary>
            public int DailyFreeTickets { get; private set; }

            /// <summary>
            /// Gets the maximum number of tickets a player can hold.
            /// </summary>
            public int MaxTickets { get; private set; }

            /// <summary>
            /// Gets the number of blocks between ticket resets.
            /// </summary>
            public int ResetIntervalBlocks { get; private set; }

            /// <summary>
            /// Gets the starting floor number for this schedule.
            /// </summary>
            public int FloorBegin { get; private set; }

            /// <summary>
            /// Gets the ending floor number for this schedule.
            /// </summary>
            public int FloorEnd { get; private set; }

            /// <summary>
            /// Gets the total number of floors in this schedule.
            /// </summary>
            public int FloorsCount => FloorEnd - FloorBegin + 1;

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                InfiniteTowerId = ParseInt(fields[1]);
                StartBlockIndex = ParseLong(fields[2]);
                EndBlockIndex = ParseLong(fields[3]);
                DailyFreeTickets = ParseInt(fields[4]);
                MaxTickets = ParseInt(fields[5]);
                ResetIntervalBlocks = ParseInt(fields[6]);
                FloorBegin = fields.Count > 7 ? ParseInt(fields[7]) : 1;
                FloorEnd = fields.Count > 8 ? ParseInt(fields[8]) : int.MaxValue;
            }

            /// <summary>
            /// Checks if the infinite tower schedule is currently active.
            /// </summary>
            /// <param name="currentBlockIndex">Current block index</param>
            /// <returns>True if the schedule is active, false otherwise</returns>
            public bool IsActive(long currentBlockIndex)
            {
                return currentBlockIndex >= StartBlockIndex && currentBlockIndex <= EndBlockIndex;
            }

            /// <summary>
            /// Checks if the infinite tower schedule has started.
            /// </summary>
            /// <param name="currentBlockIndex">Current block index</param>
            /// <returns>True if the schedule has started, false otherwise</returns>
            public bool HasStarted(long currentBlockIndex)
            {
                return currentBlockIndex >= StartBlockIndex;
            }

            /// <summary>
            /// Checks if the infinite tower schedule has ended.
            /// </summary>
            /// <param name="currentBlockIndex">Current block index</param>
            /// <returns>True if the schedule has ended, false otherwise</returns>
            public bool HasEnded(long currentBlockIndex)
            {
                return currentBlockIndex > EndBlockIndex;
            }

            /// <summary>
            /// Gets the remaining blocks until the schedule ends.
            /// </summary>
            /// <param name="currentBlockIndex">Current block index</param>
            /// <returns>Number of blocks remaining, or 0 if already ended</returns>
            public long GetRemainingBlocks(long currentBlockIndex)
            {
                if (currentBlockIndex >= EndBlockIndex)
                    return 0;

                return EndBlockIndex - currentBlockIndex;
            }

            /// <summary>
            /// Validates that the infinite tower ID matches this schedule's configuration.
            /// </summary>
            /// <param name="expectedInfiniteTowerId">The expected infinite tower ID</param>
            /// <param name="addressesHex">Addresses hex for logging</param>
            /// <exception cref="SheetRowNotFoundException">Thrown when infinite tower ID mismatch</exception>
            public void ValidateInfiniteTowerId(int expectedInfiniteTowerId, string addressesHex)
            {
                if (InfiniteTowerId != expectedInfiniteTowerId)
                {
                    throw new SheetRowNotFoundException(
                        addressesHex,
                        nameof(InfiniteTowerScheduleSheet),
                        $"InfiniteTowerId mismatch. Expected: {expectedInfiniteTowerId}, Found: {InfiniteTowerId}");
                }
            }

            /// <summary>
            /// Validates schedule timing for the infinite tower.
            /// </summary>
            /// <param name="currentBlockIndex">Current block index</param>
            /// <param name="infiniteTowerId">The infinite tower ID for error messages</param>
            /// <param name="addressesHex">Addresses hex for logging</param>
            /// <exception cref="InfiniteTowerScheduleNotStartedException">Thrown when schedule has not started</exception>
            /// <exception cref="InfiniteTowerScheduleEndedException">Thrown when schedule has ended</exception>
            /// <exception cref="InfiniteTowerScheduleNotActiveException">Thrown when schedule is not active</exception>
            public void ValidateScheduleTiming(
                long currentBlockIndex,
                int infiniteTowerId,
                string addressesHex)
            {
                // Check if schedule has started
                if (!HasStarted(currentBlockIndex))
                {
                    throw new InfiniteTowerScheduleNotStartedException(
                        "InfiniteTowerBattle",
                        addressesHex,
                        infiniteTowerId,
                        currentBlockIndex,
                        StartBlockIndex);
                }

                // Check if schedule has ended
                if (HasEnded(currentBlockIndex))
                {
                    throw new InfiniteTowerScheduleEndedException(
                        "InfiniteTowerBattle",
                        addressesHex,
                        infiniteTowerId,
                        currentBlockIndex,
                        EndBlockIndex);
                }

                // Check if schedule is currently active
                if (!IsActive(currentBlockIndex))
                {
                    throw new InfiniteTowerScheduleNotActiveException(
                        "InfiniteTowerBattle",
                        addressesHex,
                        infiniteTowerId,
                        currentBlockIndex,
                        StartBlockIndex,
                        EndBlockIndex);
                }
            }

            /// <summary>
            /// Validates that the floor ID is within this schedule's floor range.
            /// </summary>
            /// <param name="floorId">Floor ID to validate</param>
            /// <param name="addressesHex">Addresses hex for logging</param>
            /// <exception cref="InvalidOperationException">Thrown when floor ID is out of range</exception>
            public void ValidateFloorRange(int floorId, string addressesHex)
            {
                if (floorId < FloorBegin || floorId > FloorEnd)
                {
                    throw new InvalidOperationException(
                        $"[InfiniteTowerBattle][{addressesHex}] Floor {floorId} is out of range. " +
                        $"Valid floor range for this schedule is {FloorBegin}-{FloorEnd}");
                }
            }
        }
    }
}
