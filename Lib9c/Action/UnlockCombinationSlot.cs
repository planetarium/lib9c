using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Arena;
using Nekoyume.Extensions;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class UnlockCombinationSlot : GameAction
    {
        private const string TypeIdentifier = "unlock_combination_slot";
        
        public Address AvatarAddress;
        public int SlotIndex;
        
        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    ["a"] = AvatarAddress.Serialize(),
                    ["s"] = SlotIndex.Serialize(),
                }
                .ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            SlotIndex = plainValue["s"].ToInteger();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            // TODO: cost sheet
            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                });

            if (CombinationSlotState.ValidateSlotIndex(SlotIndex))
            {
                throw new InvalidSlotIndexException($"[{nameof(UnlockRuneSlot)}] Index : {SlotIndex}");
            }
            
            var allSlotState = states.GetCombinationSlotState(AvatarAddress, out _);
            var hasCombinationSlotState = allSlotState.TryGetCombinationSlotState(SlotIndex, out var combinationSlot);
            if (hasCombinationSlotState && (combinationSlot?.IsUnlocked ?? false))
            {
                throw new SlotAlreadyUnlockedException($"[{nameof(UnlockRuneSlot)}] Index : {SlotIndex}");
            }

            var feeStoreAddress = GetFeeStoreAddress(states, context.BlockIndex);
            // 코스트 계산

            allSlotState.UnlockSlot(AvatarAddress, SlotIndex);

            return states
                // .TransferAsset(context, context.Signer, feeStoreAddress, cost * currency)
                .SetCombinationSlotState(AvatarAddress, allSlotState);
        }

        private Address GetFeeStoreAddress(IWorld states, long blockIndex)
        {
            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                });
            
            var arenaSheet = sheets.GetSheet<ArenaSheet>();
            var arenaData = arenaSheet.GetRoundByBlockIndex(blockIndex);
            return ArenaHelper.DeriveArenaAddress(arenaData.ChampionshipId, arenaData.Round);
        }
    }
}
