using Bencodex.Types;
using Lib9c.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Module
{
    /// <summary>
    /// Provides utility methods for handling patrol reward states.
    /// </summary>
    public static class PatrolRewardModule
    {
        /// <summary>
        /// Retrieves the block index at which the patrol reward was last claimed for a given avatar.
        /// </summary>
        /// <param name="worldState">The current world state.</param>
        /// <param name="avatarAddress">The address of the avatar.</param>
        /// <returns>The block index when the patrol reward was last claimed.</returns>
        /// <exception cref="FailedLoadStateException">Thrown if the state could not be loaded.</exception>
        public static long GetPatrolRewardClaimedBlockIndex(this IWorldState worldState, Address avatarAddress)
        {
            var value = worldState.GetAccountState(Addresses.PatrolReward).GetState(avatarAddress);
            if (value is Integer integer)
            {
                return integer;
            }

            throw new FailedLoadStateException("Failed to load the patrol reward claimed block index state.");
        }

        /// <summary>
        /// Tries to retrieve the block index at which the patrol reward was last claimed for a given avatar.
        /// </summary>
        /// <param name="worldState">The current world state.</param>
        /// <param name="avatarAddress">The address of the avatar.</param>
        /// <param name="blockIndex">Outputs the block index when the patrol reward was last claimed.</param>
        /// <returns><c>true</c> if the block index was successfully retrieved; otherwise, <c>false</c>.</returns>
        public static bool TryGetPatrolRewardClaimedBlockIndex(this IWorldState worldState, Address avatarAddress, out long blockIndex)
        {
            blockIndex = 0L;
            try
            {
                var temp = GetPatrolRewardClaimedBlockIndex(worldState, avatarAddress);
                blockIndex = temp;
                return true;
            }
            catch (FailedLoadStateException)
            {
                return false;
            }
        }

        /// <summary>
        /// Sets the block index at which the patrol reward was last claimed for a given avatar.
        /// </summary>
        /// <param name="world">The current world instance.</param>
        /// <param name="avatarAddress">The address of the avatar.</param>
        /// <param name="blockIndex">The block index to set.</param>
        /// <returns>The updated world instance.</returns>
        public static IWorld SetPatrolRewardClaimedBlockIndex(this IWorld world, Address avatarAddress, long blockIndex)
        {
            var account = world.GetAccount(Addresses.PatrolReward);
            account = account.SetState(avatarAddress, (Integer)blockIndex);
            return world.SetAccount(Addresses.PatrolReward, account);
        }
    }
}
