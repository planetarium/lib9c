using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Nekoyume.Module
{
    public static class ProficiencyModule
    {
        public static int GetProficiency(this IWorldState state, Address avatarAddress)
        {
            var account = state.GetAccountState(Addresses.Proficiency);
            return account.GetState(avatarAddress) is Integer p ? p : 0;
        }

        public static IWorld SetProficiency(this IWorld world, Address avatarAddress,
            int proficiency)
        {
            var account = world.GetAccount(Addresses.Proficiency);
            account = account.SetState(avatarAddress, (Integer)proficiency);
            return world.SetAccount(Addresses.Proficiency, account);
        }
    }
}
