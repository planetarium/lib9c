using System;

namespace Nekoyume.Action.Exceptions
{
    /// <summary>
    /// Exception thrown when infinite tower schedule has not started yet.
    /// </summary>
    public class InfiniteTowerScheduleNotStartedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the InfiniteTowerScheduleNotStartedException class.
        /// </summary>
        /// <param name="actionType">The type of action that failed.</param>
        /// <param name="addressesHex">The hex representation of addresses involved.</param>
        /// <param name="infiniteTowerId">The infinite tower ID.</param>
        /// <param name="currentBlockIndex">The current block index.</param>
        /// <param name="startBlockIndex">The block index when the schedule starts.</param>
        public InfiniteTowerScheduleNotStartedException(
            string actionType,
            string addressesHex,
            int infiniteTowerId,
            long currentBlockIndex,
            long startBlockIndex)
            : base($"{actionType} {addressesHex} Infinite tower {infiniteTowerId} schedule has not started yet. " +
                   $"Current: {currentBlockIndex}, Starts at: {startBlockIndex}")
        {
        }
    }
}
