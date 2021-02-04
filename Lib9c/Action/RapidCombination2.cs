using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("rapid_combination2")]
    public class RapidCombination2 : GameAction
    {
        public Address avatarAddress;
        public int slotIndex;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var slotAddress = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );
            if (context.Rehearsal)
            {
                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged);
            }
            
            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!states.TryGetAgentAvatarStates(
                context.Signer,
                avatarAddress,
                out var agentState,
                out var avatarState))
            {
                var exc = new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
                Log.Error(exc.Message);
                throw exc;
            }

            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState?.Result is null)
            {
                var exc = new CombinationSlotResultNullException($"{addressesHex}CombinationSlot Result is null. ({avatarAddress}), ({slotIndex})");
                Log.Error(exc.Message);
                throw exc;
            }

            if(!avatarState.worldInformation.IsStageCleared(slotState.UnlockStage))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                var exc = new NotEnoughClearedStageLevelException(addressesHex, slotState.UnlockStage, current);
                Log.Error(exc.Message);
                throw exc;
            }

            var diff = slotState.Result.itemUsable.RequiredBlockIndex - context.BlockIndex;
            if (diff <= 0)
            {
                var exc = new RequiredBlockIndexException($"{addressesHex}Already met the required block index. context block index: {context.BlockIndex}, required block index: {slotState.Result.itemUsable.RequiredBlockIndex}");
                Log.Error(exc.Message);
                throw exc;
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                var exc = new FailedLoadStateException($"{addressesHex}Aborted as the GameConfigState was failed to load.");
                Log.Error(exc.Message);
                throw exc;
            }

            var count = RapidCombination.CalculateHourglassCount(gameConfigState, diff);
            var materialItemSheet = states.GetSheet<MaterialItemSheet>();
            var row = materialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Hourglass);
            var hourGlass = ItemFactory.CreateMaterial(row);
            if (!avatarState.inventory.RemoveFungibleItem(hourGlass, count))
            {
                var exc = new NotEnoughMaterialException(
                    $"{addressesHex}Aborted as the player has no enough material ({row.Id} * {count})");
                Log.Error(exc.Message);
                throw exc;
            }

            slotState.Update(context.BlockIndex, hourGlass, count);
            avatarState.UpdateFromRapidCombination(
                (CombinationConsumable.ResultModel) slotState.Result,
                context.BlockIndex
            );
            return states
                .SetState(avatarAddress, avatarState.Serialize())
                .SetState(slotAddress, slotState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["slotIndex"] = slotIndex.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            slotIndex = plainValue["slotIndex"].ToInteger();
        }
    }
}
