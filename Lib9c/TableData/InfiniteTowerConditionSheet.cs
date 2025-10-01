using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using Nekoyume.Model.Skill;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Infinite tower condition sheet for managing battle conditions and restrictions.
    /// Uses StatType, SkillTargetType, and StatModifier.OperationType for flexible stat modifications.
    /// </summary>
    [Serializable]
    public class InfiniteTowerConditionSheet : Sheet<int, InfiniteTowerConditionSheet.Row>
    {
        public InfiniteTowerConditionSheet() : base(nameof(InfiniteTowerConditionSheet)) { }

        /// <summary>
        /// Represents a row in the InfiniteTowerConditionSheet containing condition data.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<int>
        {
            /// <summary>
            /// Gets the condition ID as the key for this row.
            /// </summary>
            public override int Key => Id;

            /// <summary>
            /// Gets the unique identifier for this condition.
            /// </summary>
            public int Id { get; private set; }

            /// <summary>
            /// Gets the stat type that this condition affects.
            /// </summary>
            public StatType StatType { get; private set; }

            /// <summary>
            /// Gets the target type for this condition (Self, Enemy, etc.).
            /// </summary>
            public SkillTargetType TargetType { get; private set; }

            /// <summary>
            /// Gets the operation type for the stat modification.
            /// </summary>
            public StatModifier.OperationType OperationType { get; private set; }

            /// <summary>
            /// Gets the value for the stat modification.
            /// </summary>
            public int Value { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                StatType = (StatType)ParseInt(fields[1]);
                TargetType = (SkillTargetType)ParseInt(fields[2]);
                OperationType = (StatModifier.OperationType)ParseInt(fields[3]);
                Value = ParseInt(fields[4]);
            }

            /// <summary>
            /// Creates a StatModifier from this condition row.
            /// </summary>
            public StatModifier GetStatModifier()
            {
                return new StatModifier(StatType, OperationType, Value);
            }
        }
    }
}
