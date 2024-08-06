using System;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Nekoyume.Extensions
{
    public static class WorldExtensions
    {
        public static IWorld MutateAccount(this IWorld world, Address accountAddress,
            Func<IAccount, IAccount> mutateFn)
        {
            var account = world.GetAccount(accountAddress);
            account = mutateFn(account);
            return world.SetAccount(accountAddress, account);
        }
    }
}
