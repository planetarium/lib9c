using System;
using Libplanet.Action;
using Nekoyume.Model.Stat;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.InfiniteTower
{
    /// <summary>
    /// Infinite tower condition for managing battle conditions and restrictions.
    /// Uses StatType, SkillTargetType, and StatModifier for flexible stat modifications.
    /// </summary>
    [Serializable]
    public class InfiniteTowerCondition
    {
        /// <summary>
        /// Gets the unique identifier for this condition.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the stat type that this condition affects.
        /// </summary>
        public StatType StatType { get; }

        /// <summary>
        /// Gets the target type for this condition (Self, Enemy, etc.).
        /// </summary>
        public SkillTargetType TargetType { get; }

        /// <summary>
        /// Gets the operation type for the stat modification.
        /// </summary>
        public StatModifier.OperationType OperationType { get; }

        /// <summary>
        /// Gets the value for the stat modification.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Initializes a new instance of the InfiniteTowerCondition class.
        /// </summary>
        /// <param name="row">The condition sheet row containing the condition data.</param>
        public InfiniteTowerCondition(InfiniteTowerConditionSheet.Row row)
        {
            Id = row.Id;
            StatType = row.StatType;
            TargetType = row.TargetType;
            OperationType = row.OperationType;
            Value = row.Value;
        }

        /// <summary>
        /// Gets the value of this condition.
        /// </summary>
        /// <returns>The condition value.</returns>
        public int GetValue()
        {
            return Value;
        }

        /// <summary>
        /// Checks if this condition applies to the given equipment for stat modification.
        /// This is only used for stat modifiers, not equipment restrictions.
        /// </summary>
        public bool IsApplicableToEquipment(Item.Equipment equipment)
        {
            // For stat modifiers, we typically apply to all equipment
            // Specific targeting can be implemented based on condition type
            return true;
        }

        /// <summary>
        /// Checks if this condition applies to the given rune for stat modification.
        /// This is only used for stat modifiers, not rune restrictions.
        /// </summary>
        public bool IsApplicableToRune(RuneState runeState)
        {
            // For stat modifiers, we typically apply to all runes
            // Specific targeting can be implemented based on condition type
            return true;
        }

        /// <summary>
        /// Creates a StatModifier from this condition.
        /// </summary>
        public StatModifier GetStatModifier()
        {
            return new StatModifier(StatType, OperationType, Value);
        }
    }
}
