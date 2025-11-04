using System;

namespace Nekoyume.Action.Exceptions
{
    /// <summary>
    /// Exception thrown when player's combat power doesn't meet the floor requirements.
    /// </summary>
    public class CombatPowerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the CombatPowerException class.
        /// </summary>
        /// <param name="actionType">The type of action that failed.</param>
        /// <param name="addressesHex">The hex representation of addresses involved.</param>
        /// <param name="requiredCp">The required combat power.</param>
        /// <param name="currentCp">The current combat power.</param>
        /// <param name="isInsufficient">Whether the combat power is insufficient (true) or excessive (false).</param>
        public CombatPowerException(
            string actionType,
            string addressesHex,
            long requiredCp,
            long currentCp,
            bool isInsufficient = true)
            : base($"{actionType} {addressesHex} {(isInsufficient ? "Insufficient" : "Excessive")} combat power. " +
                   $"{(isInsufficient ? "Required" : "Maximum")}: {requiredCp}, Current: {currentCp}")
        {
        }
    }
}
