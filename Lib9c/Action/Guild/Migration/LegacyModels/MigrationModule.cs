using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;

namespace Nekoyume.Action.Guild.Migration.LegacyModels
{
    public static class MigrationModule
    {
        /// <summary>
        /// An address for delegation height migration.
        /// </summary>
        public static readonly Address DelegationMigrationHeight
            = new Address("0000000000000000000000000000000000000000");

        public static long? GetDelegationMigrationHeight(this IWorldState worldState)
            => worldState
                .GetAccountState(Addresses.Migration)
                .GetState(DelegationMigrationHeight) is Integer height
                    ? height
                    : null;

        public static IWorld SetDelegationMigrationHeight(this IWorld world, long height)
        {
            return world
                .MutateAccount(
                    Addresses.Migration,
                    account => account.SetState(
                        DelegationMigrationHeight,
                        (Integer)height));
        }
    }
}
