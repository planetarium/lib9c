using System;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Module
{
    public static class InfiniteTowerModule
    {
        /// <summary>
        /// Address of the board state of the given tower in <see cref="Addresses.InfiniteTowerBoard"/>.
        /// </summary>
        /// <remarks>
        /// The hex string must be lower-case. <see cref="Address(string)"/> validates the EIP-55 checksum
        /// of any string containing upper-case letters, so an upper-case form (<c>X40</c>) throws
        /// "Address checksum is invalid" for most ids with a hex digit A-F (e.g. 11).
        /// Lower-case yields the same bytes as before, so existing states stay reachable.
        /// </remarks>
        public static Address GetBoardStateAddress(int infiniteTowerId) =>
            new Address($"{infiniteTowerId:x40}");

        /// <summary>
        /// Get infinite tower info for a specific avatar and tower.
        /// </summary>
        /// <param name="worldState">The world state.</param>
        /// <param name="avatarAddress">The avatar address.</param>
        /// <param name="infiniteTowerId">The infinite tower ID.</param>
        /// <returns>InfiniteTowerInfo for the avatar and tower.</returns>
        public static InfiniteTowerInfo GetInfiniteTowerInfo(this IWorld worldState, Address avatarAddress, int infiniteTowerId)
        {
            var accountAddress = Addresses.InfiniteTowerInfo.Derive($"{infiniteTowerId}");
            var account = worldState.GetAccountState(accountAddress);
            var key = avatarAddress;
            var infiniteTowerInfoValue = account.GetState(key);

            if (infiniteTowerInfoValue is Bencodex.Types.List serializedInfiniteTowerInfoList)
            {
                return new InfiniteTowerInfo(serializedInfiniteTowerInfoList);
            }

            // Get initial tickets from schedule sheet
            var initialTickets = 0;
            try
            {
                var scheduleSheet = worldState.GetSheet<InfiniteTowerScheduleSheet>();
                var scheduleRow = scheduleSheet.Values.FirstOrDefault(s => s.InfiniteTowerId == infiniteTowerId);
                if (scheduleRow != null)
                {
                    initialTickets = Math.Min(scheduleRow.DailyFreeTickets, scheduleRow.MaxTickets);
                }
            }
            catch
            {
                // If schedule sheet is not available, use default value (0)
            }

            return new InfiniteTowerInfo(avatarAddress, infiniteTowerId, initialTickets);
        }

        /// <summary>
        /// Set infinite tower info for a specific avatar and tower.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="avatarAddress">The avatar address.</param>
        /// <param name="infiniteTowerInfo">The infinite tower info to set.</param>
        /// <returns>Updated world with the infinite tower info.</returns>
        public static IWorld SetInfiniteTowerInfo(this IWorld world, Address avatarAddress, InfiniteTowerInfo infiniteTowerInfo)
        {
            var accountAddress = Addresses.InfiniteTowerInfo.Derive($"{infiniteTowerInfo.InfiniteTowerId}");
            var account = world.GetAccount(accountAddress);
            var key = avatarAddress;
            account = account.SetState(key, infiniteTowerInfo.Serialize());
            return world.SetAccount(accountAddress, account);
        }

        /// <summary>
        /// Try to get infinite tower info for a specific avatar and tower.
        /// </summary>
        /// <param name="worldState">The world state.</param>
        /// <param name="avatarAddress">The avatar address.</param>
        /// <param name="infiniteTowerId">The infinite tower ID.</param>
        /// <param name="infiniteTowerInfo">The infinite tower info if found.</param>
        /// <returns>True if the infinite tower info was found, false otherwise.</returns>
        public static bool TryGetInfiniteTowerInfo(this IWorld worldState, Address avatarAddress, int infiniteTowerId, out InfiniteTowerInfo infiniteTowerInfo)
        {
            try
            {
                infiniteTowerInfo = GetInfiniteTowerInfo(worldState, avatarAddress, infiniteTowerId);
                return true;
            }
            catch (FailedLoadStateException)
            {
                infiniteTowerInfo = null;
                return false;
            }
        }

        /// <summary>
        /// Get infinite tower board state for a specific tower.
        /// </summary>
        /// <param name="worldState">The world state.</param>
        /// <param name="infiniteTowerId">The infinite tower ID.</param>
        /// <returns>InfiniteTowerBoardState for the tower.</returns>
        public static InfiniteTowerBoardState GetInfiniteTowerBoardState(this IWorld worldState, int infiniteTowerId)
        {
            var account = worldState.GetAccountState(Addresses.InfiniteTowerBoard);
            var seasonAddress = GetBoardStateAddress(infiniteTowerId);
            var boardStateValue = account.GetState(seasonAddress);

            return boardStateValue is Bencodex.Types.List serializedBoardState
                ? new InfiniteTowerBoardState(serializedBoardState)
                : new InfiniteTowerBoardState(infiniteTowerId);
        }

        /// <summary>
        /// Set infinite tower board state for a specific tower.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="infiniteTowerBoardState">The infinite tower board state to set.</param>
        /// <returns>Updated world with the infinite tower board state.</returns>
        public static IWorld SetInfiniteTowerBoardState(this IWorld world, InfiniteTowerBoardState infiniteTowerBoardState)
        {
            var account = world.GetAccount(Addresses.InfiniteTowerBoard);
            var seasonAddress = GetBoardStateAddress(infiniteTowerBoardState.InfiniteTowerId);
            account = account.SetState(seasonAddress, infiniteTowerBoardState.Serialize());
            return world.SetAccount(Addresses.InfiniteTowerBoard, account);
        }

        /// <summary>
        /// Try to get infinite tower board state for a specific tower.
        /// </summary>
        /// <param name="worldState">The world state.</param>
        /// <param name="infiniteTowerId">The infinite tower ID.</param>
        /// <param name="infiniteTowerBoardState">The infinite tower board state if found.</param>
        /// <returns>True if the infinite tower board state was found, false otherwise.</returns>
        public static bool TryGetInfiniteTowerBoardState(this IWorld worldState, int infiniteTowerId, out InfiniteTowerBoardState infiniteTowerBoardState)
        {
            try
            {
                infiniteTowerBoardState = GetInfiniteTowerBoardState(worldState, infiniteTowerId);
                return true;
            }
            catch (FailedLoadStateException)
            {
                infiniteTowerBoardState = null;
                return false;
            }
        }

        /// <summary>
        /// Update infinite tower board state by recording a floor clear.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="infiniteTowerId">The infinite tower ID.</param>
        /// <param name="floorId">The floor ID that was cleared.</param>
        /// <param name="blockIndex">The block index when the floor was cleared.</param>
        /// <returns>Updated world with the recorded floor clear.</returns>
        public static IWorld RecordFloorClear(this IWorld world, int infiniteTowerId, int floorId, long blockIndex)
        {
            var boardState = GetInfiniteTowerBoardState(world, infiniteTowerId);
            boardState.RecordFloorClear(floorId, blockIndex);
            return SetInfiniteTowerBoardState(world, boardState);
        }

        /// <summary>
        /// Get the clear count for a specific floor in a specific tower.
        /// </summary>
        /// <param name="worldState">The world state.</param>
        /// <param name="infiniteTowerId">The infinite tower ID.</param>
        /// <param name="floorId">The floor ID.</param>
        /// <returns>The number of times the floor has been cleared.</returns>
        public static int GetFloorClearCount(this IWorld worldState, int infiniteTowerId, int floorId)
        {
            var boardState = GetInfiniteTowerBoardState(worldState, infiniteTowerId);
            return boardState.GetFloorClearCount(floorId);
        }
    }
}
