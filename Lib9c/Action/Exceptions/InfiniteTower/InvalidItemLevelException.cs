using System;

namespace Nekoyume.Action.Exceptions
{
    /// <summary>
    /// Exception thrown when an item level doesn't meet the floor requirements.
    /// </summary>
    public class InvalidItemLevelException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the InvalidItemLevelException class.
        /// </summary>
        /// <param name="actionType">The type of action that failed.</param>
        /// <param name="addressesHex">The hex representation of addresses involved.</param>
        /// <param name="itemLevel">The item level that failed validation.</param>
        /// <param name="requiredLevel">The required level.</param>
        /// <param name="isMinimum">Whether the requirement is a minimum (true) or maximum (false).</param>
        public InvalidItemLevelException(
            string actionType,
            string addressesHex,
            int itemLevel,
            int requiredLevel,
            bool isMinimum = true)
            : base($"{actionType} {addressesHex} Invalid item level. " +
                   $"Item level '{itemLevel}' does not meet {(isMinimum ? "minimum" : "maximum")} requirement. " +
                   $"{(isMinimum ? "Minimum" : "Maximum")} level required: {requiredLevel}")
        {
        }
    }
}
