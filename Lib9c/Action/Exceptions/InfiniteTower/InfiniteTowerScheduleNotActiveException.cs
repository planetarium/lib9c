using System;

namespace Nekoyume.Action.Exceptions
{
    /// <summary>
    /// Exception thrown when infinite tower schedule is not active.
    /// </summary>
    public class InfiniteTowerScheduleNotActiveException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the InfiniteTowerScheduleNotActiveException class.
        /// </summary>
        /// <param name="actionType">The type of action that failed.</param>
        /// <param name="addressesHex">The hex representation of addresses involved.</param>
        /// <param name="infiniteTowerId">The infinite tower ID.</param>
        /// <param name="currentBlockIndex">The current block index.</param>
        /// <param name="startBlockIndex">The block index when the schedule starts.</param>
        /// <param name="endBlockIndex">The block index when the schedule ends.</param>
        public InfiniteTowerScheduleNotActiveException(
            string actionType,
            string addressesHex,
            int infiniteTowerId,
            long currentBlockIndex,
            long startBlockIndex,
            long endBlockIndex)
            : base($"{actionType} {addressesHex} Infinite tower {infiniteTowerId} schedule is not active. " +
                   $"Current: {currentBlockIndex}, Schedule: {startBlockIndex} - {endBlockIndex}")
        {
        }
    }
}
