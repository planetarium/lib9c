using System;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    /// <summary>
    /// Represents rune slot information for a character.
    /// </summary>
    [Serializable]
    public class RuneSlotInfo
    {
        /// <summary>
        /// Gets the slot index for the rune.
        /// </summary>
        public int SlotIndex { get; }

        /// <summary>
        /// Gets the rune ID equipped in this slot.
        /// </summary>
        public int RuneId { get; }

        /// <summary>
        /// Initializes a new instance of the RuneSlotInfo class.
        /// </summary>
        /// <param name="slotIndex">The slot index.</param>
        /// <param name="runeId">The rune ID.</param>
        public RuneSlotInfo(int slotIndex, int runeId)
        {
            SlotIndex = slotIndex;
            RuneId = runeId;
        }

        /// <summary>
        /// Initializes a new instance of the RuneSlotInfo class from serialized data.
        /// </summary>
        /// <param name="serialized">The serialized data.</param>
        public RuneSlotInfo(List serialized)
        {
            SlotIndex = serialized[0].ToInteger();
            RuneId = serialized[1].ToInteger();
        }

        /// <summary>
        /// Serializes the rune slot info to Bencodex format.
        /// </summary>
        /// <returns>The serialized data.</returns>
        public IValue Serialize()
        {
            return List.Empty
                .Add(SlotIndex.Serialize())
                .Add(RuneId.Serialize());
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is RuneSlotInfo other)
            {
                return SlotIndex == other.SlotIndex && RuneId == other.RuneId;
            }
            return false;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(SlotIndex, RuneId);
        }
    }
}
