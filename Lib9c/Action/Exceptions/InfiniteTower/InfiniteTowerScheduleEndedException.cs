using System;

namespace Nekoyume.Action.Exceptions
{
    /// <summary>
    /// Exception thrown when infinite tower schedule has ended.
    /// </summary>
    public class InfiniteTowerScheduleEndedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the InfiniteTowerScheduleEndedException class.
        /// </summary>
        /// <param name="actionType">The type of action that failed.</param>
        /// <param name="addressesHex">The hex representation of addresses involved.</param>
        /// <param name="infiniteTowerId">The infinite tower ID.</param>
        /// <param name="currentBlockIndex">The current block index.</param>
        /// <param name="endBlockIndex">The block index when the schedule ended.</param>
        public InfiniteTowerScheduleEndedException(
            string actionType,
            string addressesHex,
            int infiniteTowerId,
            long currentBlockIndex,
            long endBlockIndex)
            : base($"{actionType} {addressesHex} Infinite tower {infiniteTowerId} schedule has ended. " +
                   $"Current: {currentBlockIndex}, Ended at: {endBlockIndex}")
        {
        }
    }
}
