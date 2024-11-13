using Bencodex.Types;
using Libplanet.Action.State;
using Nekoyume.Extensions;

namespace Nekoyume.Action.Guild.Migration.LegacyModels
{
    public static class MigrationModule
    {
        public static long? GetDelegationMigrationHeight(this IWorldState worldState)
            => worldState
                .GetAccountState(Addresses.Migration)
                .GetState(Addresses.DelegationMigrationHeight) is Integer height
                    ? height
                    : null;

        public static IWorld SetDelegationMigrationHeight(this IWorld world, long height)
            => world
                .MutateAccount(
                    Addresses.Migration,
                    account => account.SetState(
                        Addresses.DelegationMigrationHeight,
                        (Integer)height));
    }
}
