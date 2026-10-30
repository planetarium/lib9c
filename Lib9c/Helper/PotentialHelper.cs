using System;
using System.Collections.Generic;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    /// <summary>
    /// Helper for rolling equipment potential options during the granting phase.
    /// </summary>
    public static class PotentialHelper
    {
        /// <summary>
        /// Rolls potential options for the given equipment, filling <paramref name="slotCount"/> slots.
        /// Each slot is a weighted-random pick from the option pool restricted to the equipment's
        /// sub type, with a value rolled uniformly within the selected option's inclusive
        /// [<see cref="EquipmentPotentialOptionPoolSheet.Row.ValueMin"/>,
        /// <see cref="EquipmentPotentialOptionPoolSheet.Row.ValueMax"/>] range.
        /// The result is fully deterministic given <paramref name="random"/>.
        /// </summary>
        /// <param name="equipment">The equipment to roll options for.</param>
        /// <param name="slotCount">The number of option slots to fill.</param>
        /// <param name="poolSheet">The option pool sheet.</param>
        /// <param name="random">The deterministic random source.</param>
        /// <returns>A new <see cref="EquipmentPotential"/> with the rolled slots.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="slotCount"/> is negative.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the pool has no eligible option for the equipment's sub type.
        /// </exception>
        public static EquipmentPotential Roll(
            Equipment equipment,
            int slotCount,
            EquipmentPotentialOptionPoolSheet poolSheet,
            IRandom random)
        {
            if (equipment is null)
            {
                throw new ArgumentNullException(nameof(equipment));
            }

            if (poolSheet is null)
            {
                throw new ArgumentNullException(nameof(poolSheet));
            }

            if (random is null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (slotCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            }

            var candidates = poolSheet.GetRowsForSubType(equipment.ItemSubType);
            if (slotCount > 0 && candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No potential options available for {equipment.ItemSubType}.");
            }

            var totalWeight = 0;
            foreach (var row in candidates)
            {
                totalWeight += row.Weight;
            }

            var slots = new List<PotentialOptionSlot>(slotCount);
            for (var i = 0; i < slotCount; i++)
            {
                var picked = PickWeighted(candidates, totalWeight, random);
                var value = random.Next(picked.ValueMin, picked.ValueMax + 1);
                slots.Add(new PotentialOptionSlot(picked.Id, value));
            }

            return new EquipmentPotential(slotCount, slots);
        }

        private static EquipmentPotentialOptionPoolSheet.Row PickWeighted(
            IReadOnlyList<EquipmentPotentialOptionPoolSheet.Row> candidates,
            int totalWeight,
            IRandom random)
        {
            // roll in [0, totalWeight) then walk the cumulative weights.
            var roll = random.Next(0, totalWeight);
            var cumulative = 0;
            foreach (var row in candidates)
            {
                cumulative += row.Weight;
                if (roll < cumulative)
                {
                    return row;
                }
            }

            // Defensive fallback: unreachable while totalWeight equals the sum of candidate weights.
            return candidates[candidates.Count - 1];
        }
    }
}
