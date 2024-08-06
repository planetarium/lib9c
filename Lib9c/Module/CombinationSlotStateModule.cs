#nullable enable

using System.Collections;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.Module
{
    /// <summary>
    /// CombinationSlotStateModule is the module to use 
    /// CombinationSlotState/AllCombinationSlotState with account.
    /// </summary>
    public static class CombinationSlotStateModule
    {
        public static AllCombinationSlotState GetCombinationSlotState(this IWorldState worldState,
            Address avatarAddress, out bool migrateRequired)
        {
            migrateRequired = false;
            var account = worldState.GetAccountState(Addresses.CombinationState);
            var serialized = account.GetState(avatarAddress);
            AllCombinationSlotState allCombinationSlotState;
            if (serialized is null)
            {
                allCombinationSlotState = AllCombinationSlotState.MigrationLegacyCombinationSlotState(avatarAddress);
                migrateRequired = true;
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
            var account = world.GetAccount(Addresses.CombinationState);
            account = account.SetState(avatarAddress, combinationSlotState.Serialize());
            return world.SetAccount(Addresses.CombinationState, account);
        }
    }
}
