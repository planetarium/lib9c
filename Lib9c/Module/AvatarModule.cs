using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.State;

namespace Nekoyume.Module
{
    public static class AvatarModule
    {
        public static AvatarState GetState(IWorld world, Address address)
        {
            var account = AccountHelper.ResolveAccount(world, Addresses.Avatar);

            // TODO: Move AccountStateExtensions to Lib9c.Modules?
            return account.GetAvatarState_(address);
        }

        public static bool TryGetState(IWorld world, Address avatar)
        {
            var account = AccountHelper.ResolveAccount(world, Addresses.Avatar);

            // TODO: Move AccountStateExtensions to Lib9c.Modules?
            return account.TryGetAvatarState(avatar);
        }

        public static IWorld SetState(IWorld world, Address agent, AvatarState state)
        {
            // TODO: Override legacy address to null state?
            var account = world.GetAccount(Addresses.Avatar);
            account = account.SetState(agent, state.Serialize());
            return world.SetAccount(account);
        }
    }
}
