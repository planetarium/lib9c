using System;

namespace Nekoyume.Action.Exceptions
{
    /// <summary>
    /// Exception thrown when player doesn't have enough infinite tower tickets.
    /// </summary>
    public class NotEnoughInfiniteTowerTicketsException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the NotEnoughInfiniteTowerTicketsException class.
        /// </summary>
        /// <param name="actionType">The type of action that failed.</param>
        /// <param name="addressesHex">The hex representation of addresses involved.</param>
        /// <param name="requiredTickets">The number of tickets required.</param>
        /// <param name="availableTickets">The number of tickets available.</param>
        public NotEnoughInfiniteTowerTicketsException(
            string actionType,
            string addressesHex,
            int requiredTickets,
            int availableTickets)
            : base($"{actionType} {addressesHex} Not enough infinite tower tickets. " +
                   $"Required: {requiredTickets}, Available: {availableTickets}")
        {
        }
    }
}
