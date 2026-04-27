using System;

namespace Nekoyume.Action.Exceptions
{
    /// <summary>
    /// Exception thrown when trying to re-enter a floor that has
    /// already been cleared in the current season.
    /// </summary>
    public class FloorAlreadyClearedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the
        /// FloorAlreadyClearedException class.
        /// </summary>
        /// <param name="actionType">The type of action that failed.</param>
        /// <param name="addressesHex">The hex representation of
        /// addresses involved.</param>
        /// <param name="floorId">The floor that was attempted.</param>
        /// <param name="clearedFloor">The highest cleared floor.</param>
        public FloorAlreadyClearedException(
            string actionType,
            string addressesHex,
            int floorId,
            int clearedFloor)
            : base(
                $"{actionType} {addressesHex} Floor {floorId}" +
                $" already cleared. Cleared floor: {clearedFloor}")
        {
        }
    }
}
