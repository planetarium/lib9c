using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Item
{
    /// <summary>
    /// Represents the latent ("potential") option layer attached to an <see cref="Equipment"/>.
    /// This is an independent layer that lives on top of the equipment's crafted stats and skills;
    /// it does not touch <see cref="Nekoyume.Model.Stat.StatsMap"/> so that each option keeps its own
    /// identity (row id + rolled value) and is fully decoupled from enhancement amplification.
    ///
    /// <para>
    /// Serialization is a self-contained List with its own version number, so the potential schema
    /// can evolve independently of the shared <see cref="ItemBase.SerializationVersion"/>.
    /// Field order (List format): [version, unlockedSlotCount, slots].
    /// </para>
    /// </summary>
    /// <remarks>
    /// Only the granting side is implemented in this phase: options are stored as
    /// <c>(optionRowId, rolledValue)</c> pairs, which are neutral with respect to how the option is
    /// later interpreted/applied to stats. Interpretation is intentionally out of scope here.
    /// </remarks>
    [Serializable]
    public class EquipmentPotential
    {
        /// <summary>
        /// Serialization version for this potential layer, independent of <see cref="ItemBase.SerializationVersion"/>.
        /// </summary>
        public const int SerializationVersion = 1;

        // Field count constant for serialization: version + unlockedSlotCount + slots.
        private const int FieldCount = 3;

        /// <summary>
        /// An empty potential layer, used as the default for equipment that has never been granted options
        /// and for legacy (Dictionary format) equipment.
        /// </summary>
        public static EquipmentPotential Empty =>
            new EquipmentPotential(0, new List<PotentialOptionSlot>());

        /// <summary>
        /// The number of slots that have been unlocked on the equipment.
        /// </summary>
        public int UnlockedSlotCount { get; }

        /// <summary>
        /// The granted option slots. Each entry stores the option row id and the rolled value.
        /// </summary>
        public IReadOnlyList<PotentialOptionSlot> Slots { get; }

        /// <summary>
        /// Whether this potential layer has no unlocked slots and no granted options.
        /// </summary>
        public bool IsEmpty => UnlockedSlotCount == 0 && Slots.Count == 0;

        /// <summary>
        /// Creates a potential layer from an unlocked slot count and a list of option slots.
        /// </summary>
        /// <param name="unlockedSlotCount">The number of unlocked slots.</param>
        /// <param name="slots">The granted option slots.</param>
        public EquipmentPotential(int unlockedSlotCount, IReadOnlyList<PotentialOptionSlot> slots)
        {
            UnlockedSlotCount = unlockedSlotCount;
            Slots = slots ?? new List<PotentialOptionSlot>();
        }

        /// <summary>
        /// Deserializes a potential layer from its List serialization.
        /// </summary>
        /// <param name="serialized">Serialized data in List format.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the format is not a List, is too short, or has an unsupported version.
        /// </exception>
        public EquipmentPotential(IValue serialized)
        {
            if (serialized is not List list)
            {
                throw new ArgumentException(
                    $"Unsupported serialization format for {nameof(EquipmentPotential)}: " +
                    $"{serialized?.GetType().Name ?? "null"}. Expected List.");
            }

            if (list.Count < FieldCount)
            {
                throw new ArgumentException(
                    $"Invalid list length for {nameof(EquipmentPotential)}: " +
                    $"expected at least {FieldCount}, got {list.Count}.");
            }

            var version = (int)((Integer)list[0]).Value;
            if (version != SerializationVersion)
            {
                throw new ArgumentException(
                    $"Unsupported serialization version for {nameof(EquipmentPotential)}: {version}. " +
                    $"Expected {SerializationVersion}.");
            }

            UnlockedSlotCount = (int)((Integer)list[1]).Value;
            Slots = ((List)list[2])
                .Select(v => new PotentialOptionSlot(v))
                .ToList();
        }

        /// <summary>
        /// Serializes the potential layer to List format.
        /// Order: [version, unlockedSlotCount, slots].
        /// </summary>
        /// <returns>List containing serialized data.</returns>
        public IValue Serialize()
        {
            return List.Empty
                .Add(SerializationVersion)
                .Add(UnlockedSlotCount)
                .Add(new List(Slots.Select(s => s.Serialize())));
        }

        protected bool Equals(EquipmentPotential other)
        {
            return UnlockedSlotCount == other.UnlockedSlotCount &&
                Slots.SequenceEqual(other.Slots);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EquipmentPotential)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = UnlockedSlotCount;
                foreach (var slot in Slots)
                {
                    hashCode = (hashCode * 397) ^ slot.GetHashCode();
                }

                return hashCode;
            }
        }
    }

    /// <summary>
    /// A single granted potential option slot on an <see cref="Equipment"/>.
    /// Stores the option pool row id and the value that was rolled at grant time; this is the minimal,
    /// application-neutral record. The interpretation of the value (stat type, modify type, etc.) is
    /// resolved later from the option pool sheet using <see cref="OptionRowId"/>.
    ///
    /// <para>
    /// Field order (List format): [optionRowId, value].
    /// </para>
    /// </summary>
    [Serializable]
    public class PotentialOptionSlot
    {
        // Field count constant for serialization: optionRowId + value.
        private const int FieldCount = 2;

        /// <summary>
        /// The option pool row id this slot was rolled from.
        /// </summary>
        public int OptionRowId { get; }

        /// <summary>
        /// The value rolled for this option at grant time. Fixed once granted, regardless of later sheet revisions.
        /// </summary>
        public decimal Value { get; }

        /// <summary>
        /// Creates an option slot from a row id and a rolled value.
        /// </summary>
        /// <param name="optionRowId">The option pool row id.</param>
        /// <param name="value">The rolled value.</param>
        public PotentialOptionSlot(int optionRowId, decimal value)
        {
            OptionRowId = optionRowId;
            Value = value;
        }

        /// <summary>
        /// Deserializes an option slot from its List serialization.
        /// </summary>
        /// <param name="serialized">Serialized data in List format.</param>
        /// <exception cref="ArgumentException">Thrown when the format is not a List or is too short.</exception>
        public PotentialOptionSlot(IValue serialized)
        {
            if (serialized is not List list)
            {
                throw new ArgumentException(
                    $"Unsupported serialization format for {nameof(PotentialOptionSlot)}: " +
                    $"{serialized?.GetType().Name ?? "null"}. Expected List.");
            }

            if (list.Count < FieldCount)
            {
                throw new ArgumentException(
                    $"Invalid list length for {nameof(PotentialOptionSlot)}: " +
                    $"expected at least {FieldCount}, got {list.Count}.");
            }

            OptionRowId = (int)((Integer)list[0]).Value;
            Value = list[1].ToDecimal();
        }

        /// <summary>
        /// Serializes the option slot to List format.
        /// Order: [optionRowId, value].
        /// </summary>
        /// <returns>List containing serialized data.</returns>
        public IValue Serialize()
        {
            return List.Empty
                .Add(OptionRowId)
                .Add(Value.Serialize());
        }

        protected bool Equals(PotentialOptionSlot other)
        {
            return OptionRowId == other.OptionRowId && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PotentialOptionSlot)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (OptionRowId * 397) ^ Value.GetHashCode();
            }
        }
    }
}
