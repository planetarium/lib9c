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
            var account = worldState.GetAccountState(Addresses.RuneState);
            var serialized = account.GetState(avatarAddress);
            AllCombinationSlotState allCombinationSlotState;
            if (serialized is null)
            {
                // Get legacy rune states
                // var runeListSheet = worldState.GetSheet<RuneListSheet>();
                // allRuneState = new AllRuneState();
                // foreach (var rune in runeListSheet.Values)
                // {
                //     var runeAddress = RuneState.DeriveAddress(avatarAddress, rune.Id);
                //     if (worldState.TryGetLegacyState(runeAddress, out List rawState))
                //     {
                //         var runeState = new RuneState(rawState);
                //         allRuneState.AddRuneState(runeState);
                //     }
                // }

                migrateRequired = true;
            }
            else
            {
                allCombinationSlotState = new AllCombinationSlotState((List)serialized);
            }

            return null;
        }
    }
}
