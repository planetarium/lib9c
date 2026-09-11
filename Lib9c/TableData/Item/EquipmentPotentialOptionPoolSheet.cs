using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// The pool of potential options that can be rolled onto equipment.
    /// Each row is a single option definition keyed by <see cref="Row.Id"/>. When an option is
    /// granted, only the row id and the rolled value are stored on the equipment
    /// (<see cref="Nekoyume.Model.Item.PotentialOptionSlot"/>); the remaining columns describe how the
    /// option is later interpreted (resolved) into a stat effect.
    ///
    /// <para>
    /// v1 supports stat options only. Additional effect kinds (content-limited damage, skills, etc.)
    /// will be added as new columns alongside a resolver, without changing how options are stored.
    /// </para>
    /// </summary>
    [Serializable]
    public class EquipmentPotentialOptionPoolSheet : Sheet<int, EquipmentPotentialOptionPoolSheet.Row>
    {
        /// <summary>
        /// A single potential option definition.
        /// Column order: id, item_sub_type, stat_type, modify_type, value_min, value_max, weight.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            /// <summary>The option id; this value is stored on the equipment when granted.</summary>
            public int Id { get; private set; }

            /// <summary>The equipment sub type this option can be rolled onto.</summary>
            public ItemSubType ItemSubType { get; private set; }

            /// <summary>The stat this option modifies when resolved.</summary>
            public StatType StatType { get; private set; }

            /// <summary>Whether the rolled value is applied additively or as a percentage when resolved.</summary>
            public StatModifier.OperationType ModifyType { get; private set; }

            /// <summary>The inclusive minimum of the rolled value.</summary>
            public int ValueMin { get; private set; }

            /// <summary>The inclusive maximum of the rolled value.</summary>
            public int ValueMax { get; private set; }

            /// <summary>The relative weight used when randomly selecting this option.</summary>
            public int Weight { get; private set; }

            /// <summary>
            /// Parses a CSV row into this option definition.
            /// </summary>
            /// <param name="fields">The CSV fields in column order.</param>
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ItemSubType = (ItemSubType)Enum.Parse(typeof(ItemSubType), fields[1]);
                StatType = (StatType)Enum.Parse(typeof(StatType), fields[2]);
                ModifyType = (StatModifier.OperationType)Enum.Parse(
                    typeof(StatModifier.OperationType), fields[3]);
                ValueMin = ParseInt(fields[4]);
                ValueMax = ParseInt(fields[5]);
                Weight = ParseInt(fields[6], 0);

                if (ValueMin > ValueMax)
                {
                    throw new ArgumentException(
                        $"{nameof(EquipmentPotentialOptionPoolSheet)} row {Id}: " +
                        $"value_min ({ValueMin}) must not exceed value_max ({ValueMax}).");
                }
            }
        }

        /// <summary>
        /// Creates an empty <see cref="EquipmentPotentialOptionPoolSheet"/>.
        /// </summary>
        public EquipmentPotentialOptionPoolSheet()
            : base(nameof(EquipmentPotentialOptionPoolSheet))
        {
        }

        /// <summary>
        /// Gets the eligible option rows (positive weight) for the given equipment sub type,
        /// ordered by id for deterministic selection.
        /// </summary>
        /// <param name="itemSubType">The equipment sub type to filter by.</param>
        /// <returns>The ordered list of eligible option rows.</returns>
        public IReadOnlyList<Row> GetRowsForSubType(ItemSubType itemSubType)
        {
            return OrderedList
                .Where(row => row.ItemSubType == itemSubType && row.Weight > 0)
                .OrderBy(row => row.Id)
                .ToList();
        }
    }
}
