using Bencodex.Types;
using Lib9c.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Module
{
    public static class DailyRewardModule
    {
        public static long GetDailyRewardReceivedBlockIndex(this IWorldState worldState,
            Address avatarAddress)
        {
            IAccountState account = worldState.GetAccountState(Addresses.DailyReward);
            var value = account.GetState(avatarAddress);
            if (value is Integer l)
            {
                return l;
            }

            throw new FailedLoadStateException("");
        }

        public static bool TryGetDailyRewardReceivedBlockIndex(this IWorldState worldState, Address avatarAddress, out long receivedBlockIndex)
        {
            receivedBlockIndex = 0L;
            try
            {
                var temp= GetDailyRewardReceivedBlockIndex(worldState, avatarAddress);
                receivedBlockIndex = temp;
                return true;
            }
            catch (FailedLoadStateException)
            {
                return false;
            }
        }

        public static IWorld SetDailyRewardReceivedBlockIndex(this IWorld world, Address avatarAddress,
            long blockIndex)
        {
            IAccount account = world.GetAccount(Addresses.DailyReward);
            account = account.SetState(avatarAddress, (Integer)blockIndex);
            return world.SetAccount(Addresses.DailyReward, account);
        }
    }
}
