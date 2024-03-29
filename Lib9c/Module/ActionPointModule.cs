using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Nekoyume.Module
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

            return 0;
        }

        public static IWorld SetActionPoint(this IWorld world, Address avatarAddress, long actionPoint)
        {
            var account = world.GetAccount(Addresses.ActionPoint);
            account = account.SetState(avatarAddress, (Integer)actionPoint);
            return world.SetAccount(Addresses.ActionPoint, account);
        }
    }
}
