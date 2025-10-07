using Bencodex.Types;
using Lib9c.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Module
{
    public static class ActionPointModule
    {
        public static long GetActionPoint(this IWorldState worldState, Address avatarAddress)
        {
            var value = worldState.GetAccountState(Addresses.ActionPoint).GetState(avatarAddress);
            if (value is Integer integer)
            {
                return integer;
            }

            throw new FailedLoadStateException("");
        }

        public static bool TryGetActionPoint(this IWorldState worldState, Address avatarAddress, out long actionPoint)
        {
            actionPoint = 0L;
            try
            {
                var temp = GetActionPoint(worldState, avatarAddress);
                actionPoint = temp;
                return true;
            }
            catch (FailedLoadStateException)
            {
                return false;
            }
        }

        public static IWorld SetActionPoint(this IWorld world, Address avatarAddress, long actionPoint)
        {
            var account = world.GetAccount(Addresses.ActionPoint);
            account = account.SetState(avatarAddress, (Integer)actionPoint);
            return world.SetAccount(Addresses.ActionPoint, account);
        }
    }
}
