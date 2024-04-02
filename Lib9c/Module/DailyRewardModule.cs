using System;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Module
{
    public static class DailyRewardModule
    {
        public class DailyRewardInfo : IBencodable
        {
            public Address AgentAddress;
            public long ReceivedBlockIndex;

            public DailyRewardInfo(Address address, long blockIndex)
            {
                AgentAddress = address;
                ReceivedBlockIndex = blockIndex;
            }

            public IValue Bencoded => List.Empty.Add(AgentAddress.Serialize()).Add(ReceivedBlockIndex);
        }

        public static DailyRewardInfo GetDailyRewardInfo(this IWorldState worldState,
            Address avatarAddress)
        {
            IAccountState account = worldState.GetAccountState(Addresses.DailyReward);
            var value = account.GetState(avatarAddress);
            if (value is List l)
            {
                return new DailyRewardInfo(l[0].ToAddress(), (Integer)l[1]);
            }

            throw new FailedLoadStateException("");
        }

        public static bool TryGetDailyRewardInfo(this IWorldState worldState,
            Address agentAddress, Address avatarAddress, out DailyRewardInfo dailyRewardInfo)
        {
            dailyRewardInfo = null;
            try
            {
                var temp= GetDailyRewardInfo(worldState, avatarAddress);
                if (!temp.AgentAddress.Equals(agentAddress))
                {
                    return false;
                }

                dailyRewardInfo = temp;
                return true;
            }
            catch (FailedLoadStateException)
            {
                return false;
            }
        }

        public static IWorld SetDailyRewardInfo(this IWorld world, Address avatarAddress,
            DailyRewardInfo dailyRewardInfo)
        {
            IAccount account = world.GetAccount(Addresses.DailyReward);
            account = account.SetState(avatarAddress, dailyRewardInfo.Bencoded);
            return world.SetAccount(Addresses.DailyReward, account);
        }
    }
}
