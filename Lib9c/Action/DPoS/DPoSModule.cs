#nullable enable
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.DPoS.Misc;

namespace Nekoyume.Action.DPoS
{
    public static class DPoSModule
    {
        public static IValue? GetDPoSState(this IWorldState world, Address address)
        {
            return world.GetAccountState(ReservedAddress.DPoSAccountAddress).GetState(address);
        }

        public static IWorld SetDPoSState(this IWorld world, Address address, IValue value)
        {
            var account = world.GetAccount(ReservedAddress.DPoSAccountAddress);
            account = account.SetState(address, value);
            return world.SetAccount(ReservedAddress.DPoSAccountAddress, account);
        }
    }
}
