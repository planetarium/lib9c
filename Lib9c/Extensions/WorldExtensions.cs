using System;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Arena;
using Nekoyume.Module;
using Nekoyume.TableData;

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

        /// <summary>
        /// Retrieves the fee address based on the block index and migration height.
        /// </summary>
        /// <param name="world">The current world instance.</param>
        /// <param name="blockIndex">The block index for which the fee address is required.</param>
        /// <returns>The fee address, which may vary based on migration height and arena data.</returns>
        public static Address GetFeeAddress(this IWorld world, long blockIndex)
        {
            // Default fee address is set to the RewardPool address.
            var feeAddress = Addresses.RewardPool;

            // Check if the block index is before the migration height to determine if an arena-specific address should be used.
            if (world.GetDelegationMigrationHeight() is long migrationHeight
                && blockIndex < migrationHeight)
            {
                // Fetch arena data from the ArenaSheet to derive the fee address.
                var arenaSheet = world.GetSheet<ArenaSheet>();
                var arenaData = arenaSheet.GetRoundByBlockIndex(blockIndex);

                // Derive the fee address based on the championship ID and round.
                feeAddress = ArenaHelper.DeriveArenaAddress(arenaData.ChampionshipId, arenaData.Round);
            }

            return feeAddress;
        }
    }
}
