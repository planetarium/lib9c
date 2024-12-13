using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;

namespace Nekoyume.Module
{
    public static class PatrolRewardModule
    {
        public static long GetPatrolRewardClaimedBlockIndex(this IWorldState worldState, Address avatarAddress)
        {
            var value = worldState.GetAccountState(Addresses.PatrolReward).GetState(avatarAddress);
            if (value is Integer integer)
            {
                return integer;
            }

            throw new FailedLoadStateException("");
        }

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

        public static IWorld SetPatrolRewardClaimedBlockIndex(this IWorld world, Address avatarAddress, long blockIndex)
        {
            var account = world.GetAccount(Addresses.PatrolReward);
            account = account.SetState(avatarAddress, (Integer)blockIndex);
            return world.SetAccount(Addresses.PatrolReward, account);
        }
    }
}
