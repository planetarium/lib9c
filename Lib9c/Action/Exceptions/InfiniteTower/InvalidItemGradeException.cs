using System;

namespace Nekoyume.Action.Exceptions
{
    /// <summary>
    /// Exception thrown when an item grade doesn't meet the floor requirements.
    /// </summary>
    public class InvalidItemGradeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the InvalidItemGradeException class.
        /// </summary>
        /// <param name="actionType">The type of action that failed.</param>
        /// <param name="addressesHex">The hex representation of addresses involved.</param>
        /// <param name="itemGrade">The item grade that failed validation.</param>
        /// <param name="requiredGrade">The required grade.</param>
        /// <param name="isMinimum">Whether the requirement is a minimum (true) or maximum (false).</param>
        public InvalidItemGradeException(
            string actionType,
            string addressesHex,
            int itemGrade,
            int requiredGrade,
            bool isMinimum = true)
            : base($"{actionType} {addressesHex} Invalid item grade. " +
                   $"Item grade '{itemGrade}' does not meet {(isMinimum ? "minimum" : "maximum")} requirement. " +
                   $"{(isMinimum ? "Minimum" : "Maximum")} grade required: {requiredGrade}")
        {
        }
    }
}
