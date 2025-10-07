#nullable enable

using Bencodex.Types;
using Lib9c.Model.State;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Module
{
    /// <summary>
    /// CombinationSlotStateModule is the module to use
    /// CombinationSlotState/AllCombinationSlotState with account.
    /// </summary>
    public static class CombinationSlotStateModule
    {
        public static AllCombinationSlotState GetAllCombinationSlotState(this IWorldState worldState,
            Address avatarAddress)
        {
            var account = worldState.GetAccountState(Addresses.CombinationSlot);
            var serialized = account.GetState(avatarAddress);
            AllCombinationSlotState allCombinationSlotState;
            if (serialized is null)
            {
                allCombinationSlotState = AllCombinationSlotState.MigrationLegacySlotState(worldState, avatarAddress);
            }
            else
            {
                allCombinationSlotState = new AllCombinationSlotState((List)serialized);
            }

            return allCombinationSlotState;
        }

        public static IWorld SetCombinationSlotState(this IWorld world, Address avatarAddress,
            AllCombinationSlotState combinationSlotState)
        {
            var account = world.GetAccount(Addresses.CombinationSlot);
            account = account.SetState(avatarAddress, combinationSlotState.Serialize());
            return world.SetAccount(Addresses.CombinationSlot, account);
        }
    }
}
