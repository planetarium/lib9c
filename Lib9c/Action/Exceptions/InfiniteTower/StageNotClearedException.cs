using System;

namespace Nekoyume.Action.Exceptions
{
    /// <summary>
    /// Exception thrown when trying to access a stage that hasn't been cleared yet.
    /// </summary>
    public class StageNotClearedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the StageNotClearedException class.
        /// </summary>
        /// <param name="actionType">The type of action that failed.</param>
        /// <param name="addressesHex">The hex representation of addresses involved.</param>
        /// <param name="requiredStage">The stage that needs to be cleared.</param>
        /// <param name="clearedStage">The highest stage that has been cleared.</param>
        public StageNotClearedException(
            string actionType,
            string addressesHex,
            int requiredStage,
            int clearedStage)
            : base($"{actionType} {addressesHex} Stage {requiredStage} not cleared. " +
                   $"Cleared stage: {clearedStage}")
        {
        }
    }
}
