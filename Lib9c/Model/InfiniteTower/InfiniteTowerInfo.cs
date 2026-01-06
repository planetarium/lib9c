using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Nekoyume.Action;

namespace Nekoyume.Model.InfiniteTower
{
    /// <summary>
    /// Infinite tower info for managing player's tower progress and tickets.
    /// </summary>
    [Serializable]
    public class InfiniteTowerInfo : IState
    {
        /// <summary>
        /// Gets the avatar address for this infinite tower info.
        /// </summary>
        public Address Address { get; }

        /// <summary>
        /// Gets the infinite tower ID this info belongs to.
        /// </summary>
        public int InfiniteTowerId { get; private set; }

        /// <summary>
        /// Gets the highest floor that has been cleared by the player.
        /// </summary>
        public int ClearedFloor { get; private set; }

        /// <summary>
        /// Gets the number of remaining tickets for the player.
        /// </summary>
        public int RemainingTickets { get; private set; }

        /// <summary>
        /// Gets the total number of tickets used by the player.
        /// </summary>
        public int TotalTicketsUsed { get; private set; }

        /// <summary>
        /// Gets the number of times the player has purchased tickets.
        /// </summary>
        public int NumberOfTicketPurchases { get; private set; }

        /// <summary>
        /// Gets the block index when the tower was last reset.
        /// </summary>
        public long LastResetBlockIndex { get; private set; }

        /// <summary>
        /// Gets the block index when tickets were last refilled.
        /// </summary>
        public long LastTicketRefillBlockIndex { get; private set; }

        /// <summary>
        /// Initializes a new instance of the InfiniteTowerInfo class.
        /// </summary>
        /// <param name="address">The avatar address.</param>
        /// <param name="infiniteTowerId">The infinite tower ID.</param>
        /// <param name="initialTickets">The initial number of tickets to grant on creation.</param>
        public InfiniteTowerInfo(Address address, int infiniteTowerId, int initialTickets)
        {
            Address = address;
            InfiniteTowerId = infiniteTowerId;
            ClearedFloor = 0;
            RemainingTickets = initialTickets;
            TotalTicketsUsed = 0;
            NumberOfTicketPurchases = 0;
            LastResetBlockIndex = 0;
            LastTicketRefillBlockIndex = 0;
        }

        /// <summary>
        /// Initializes a new instance of the InfiniteTowerInfo class from serialized data.
        /// </summary>
        /// <param name="serialized">The serialized data.</param>
        public InfiniteTowerInfo(List serialized)
        {
            Address = serialized[0].ToAddress();
            InfiniteTowerId = serialized[1].ToInteger();
            ClearedFloor = serialized[2].ToInteger();
            RemainingTickets = serialized[3].ToInteger();
            TotalTicketsUsed = serialized[4].ToInteger();
            NumberOfTicketPurchases = serialized[5].ToInteger();
            LastResetBlockIndex = serialized[6].ToLong();
            LastTicketRefillBlockIndex = serialized[7].ToLong();
        }

        public bool IsCleared(int floor)
        {
            return ClearedFloor >= floor;
        }

        public bool TryUseTickets(int count)
        {
            if (RemainingTickets < count)
            {
                return false;
            }

            RemainingTickets -= count;
            TotalTicketsUsed += count;
            return true;
        }

        public void AddTickets(int count)
        {
            RemainingTickets += count;
        }

        public void ClearFloor(int floor)
        {
            if (floor > ClearedFloor)
            {
                ClearedFloor = floor;
            }
        }

        public void IncreaseNumberOfTicketPurchases()
        {
            NumberOfTicketPurchases++;
        }

        public void ResetTickets(int maxTickets)
        {
            RemainingTickets = maxTickets;
            TotalTicketsUsed = 0;
            NumberOfTicketPurchases = 0;
            LastTicketRefillBlockIndex = 0; // Reset refill block index on full reset
        }

        /// <summary>
        /// Refills daily free tickets according to elapsed block periods since the last refill.
        /// Caps the total number of tickets to a fixed capacity (10).
        /// </summary>
        /// <param name="dailyFreeTickets">Number of free tickets to give per period (from schedule's DailyFreeTickets)</param>
        /// <param name="maxTickets">Maximum number of tickets player can hold (from schedule's MaxTickets)</param>
        /// <param name="currentBlockIndex">Current block index</param>
        /// <param name="blocksPerDay">Number of blocks per period (from schedule's ResetIntervalBlocks, default: 10800)</param>
        /// <returns>True if tickets were refilled, false otherwise</returns>
        public bool TryRefillDailyTickets(int dailyFreeTickets, int maxTickets, long currentBlockIndex, int blocksPerDay = 10800)
        {
            // If this is the first call, initialize the reference point and do not refill immediately
            if (LastTicketRefillBlockIndex == 0)
            {
                LastTicketRefillBlockIndex = currentBlockIndex;
                return false;
            }

            var elapsed = currentBlockIndex - LastTicketRefillBlockIndex;
            if (elapsed < blocksPerDay)
            {
                return false;
            }

            // Calculate how many full periods have passed and refill accordingly (cumulative)
            var periods = (int)(elapsed / blocksPerDay);
            var desiredAdd = dailyFreeTickets * periods;
            var capacityLeft = Math.Max(0, maxTickets - RemainingTickets);
            var ticketsToAdd = Math.Min(capacityLeft, desiredAdd);

            if (ticketsToAdd <= 0)
            {
                // Move forward the reference point even if no tickets are added due to capacity
                LastTicketRefillBlockIndex += periods * (long)blocksPerDay;
                return false;
            }

            RemainingTickets += ticketsToAdd;
            // Advance the reference point by full periods that were accounted for
            LastTicketRefillBlockIndex += periods * (long)blocksPerDay;
            return true;
        }

        /// <summary>
        /// Performs a season reset of the infinite tower progress.
        /// This is called when a new season starts. Grants exactly 1 ticket on season start.
        /// </summary>
        /// <param name="currentBlockIndex">Current block index</param>
        /// <param name="dailyFreeTickets">Ignored for initial grant; kept for backward-compatibility</param>
        /// <param name="maxTickets">Ignored; capacity is fixed to 10</param>
        public void PerformSeasonReset(long currentBlockIndex, int dailyFreeTickets, int maxTickets)
        {
            ClearedFloor = 0;
            RemainingTickets = 1; // Season start: grant exactly 1 ticket
            TotalTicketsUsed = 0;
            NumberOfTicketPurchases = 0;
            LastResetBlockIndex = currentBlockIndex;
            LastTicketRefillBlockIndex = currentBlockIndex;
        }

        /// <summary>
        /// Checks if the infinite tower is currently accessible based on schedule.
        /// </summary>
        /// <param name="currentBlockIndex">Current block index</param>
        /// <param name="startBlockIndex">Schedule start block index</param>
        /// <param name="endBlockIndex">Schedule end block index</param>
        /// <returns>True if accessible, false otherwise</returns>
        public bool IsAccessible(long currentBlockIndex, long startBlockIndex, long endBlockIndex)
        {
            return currentBlockIndex >= startBlockIndex && currentBlockIndex <= endBlockIndex;
        }

        /// <summary>
        /// Gets the schedule status for the infinite tower.
        /// </summary>
        /// <param name="currentBlockIndex">Current block index</param>
        /// <param name="startBlockIndex">Schedule start block index</param>
        /// <param name="endBlockIndex">Schedule end block index</param>
        /// <returns>Schedule status string</returns>
        public string GetScheduleStatus(long currentBlockIndex, long startBlockIndex, long endBlockIndex)
        {
            if (currentBlockIndex < startBlockIndex)
            {
                return $"Not started yet. Starts at block {startBlockIndex}";
            }
            else if (currentBlockIndex > endBlockIndex)
            {
                return $"Ended. Was active from block {startBlockIndex} to {endBlockIndex}";
            }
            else
            {
                var remainingBlocks = endBlockIndex - currentBlockIndex;
                return $"Active. {remainingBlocks} blocks remaining";
            }
        }

        public IValue Serialize()
        {
            return new List(
                Address.Serialize(),
                InfiniteTowerId.Serialize(),
                ClearedFloor.Serialize(),
                RemainingTickets.Serialize(),
                TotalTicketsUsed.Serialize(),
                NumberOfTicketPurchases.Serialize(),
                LastResetBlockIndex.Serialize(),
                LastTicketRefillBlockIndex.Serialize()
            );
        }

    }
}
