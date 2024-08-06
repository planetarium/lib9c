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
                // Get legacy combination slot states
                // 만약 AllCombinationSlotState가 없다면, 슬롯 확장 업데이트 전 4개의 슬롯을 가져와서 채워넣는다.
                allCombinationSlotState = new AllCombinationSlotState();
                for (var i = 0; i < AvatarState.DefaultCombinationSlotCount; i++)
                {
                    var combinationAddress = CombinationSlotState.DeriveAddress(avatarAddress, i);
                    allCombinationSlotState.AddRuneState(new CombinationSlotState(combinationAddress, i));
                }

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
