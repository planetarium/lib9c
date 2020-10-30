using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("RapidCombination exec started.");

            if (!states.TryGetAgentAvatarStates(
                context.Signer,
                avatarAddress,
                out var agentState,
                out var avatarState))
            {
                throw new FailedLoadStateException("Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Debug("RapidCombination Get AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState?.Result is null)
            {
                throw new CombinationSlotResultNullException($"CombinationSlot Result is null. ({avatarAddress}), ({slotIndex})");
            }

            sw.Stop();
            Log.Debug("RapidCombination Get Slot: {Elapsed}", sw.Elapsed);
            sw.Restart();

            if(!avatarState.worldInformation.IsStageCleared(slotState.UnlockStage))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(slotState.UnlockStage, current);
            }

            var diff = slotState.Result.itemUsable.RequiredBlockIndex - context.BlockIndex;
            if (diff <= 0)
            {
                throw new RequiredBlockIndexException($"Already met the required block index. context block index: {context.BlockIndex}, required block index: {slotState.Result.itemUsable.RequiredBlockIndex}");
            }

            sw.Stop();
            Log.Debug("RapidCombination Check BlockIndex: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException("Aborted as the GameConfigState was failed to load.");
            }

            sw.Stop();
            Log.Debug("RapidCombination Get GameConfig: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var count = RapidCombination.CalculateHourglassCount(gameConfigState, diff);

            sw.Stop();
            Log.Debug("RapidCombination CalculateHourglassCount(): {Elapsed}", sw.Elapsed);
            sw.Restart();

            var materialItemSheet = states.GetSheet<MaterialItemSheet>();
            var row = materialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Hourglass);
            var hourGlass = ItemFactory.CreateMaterial(row);

            sw.Stop();
            Log.Debug("RapidCombination CreateMaterial: {Elapsed}", sw.Elapsed);
            sw.Restart();

            if (!avatarState.inventory.RemoveFungibleItem(hourGlass, count))
            {
                throw new NotEnoughMaterialException(
                    $"Aborted as the player has no enough material ({row.Id} * {count})");
            }


            sw.Stop();
            Log.Debug("RapidCombination RemoveMaterial: {Elapsed}", sw.Elapsed);
            sw.Restart();

            slotState.Update(context.BlockIndex, hourGlass, count);

            sw.Stop();
            Log.Debug("RapidCombination Update slot: {Elapsed}", sw.Elapsed);
            sw.Restart();

            avatarState.UpdateFromRapidCombination(
                (CombinationConsumable.ResultModel) slotState.Result,
                context.BlockIndex
            );

            states = states.SetState(avatarAddress, avatarState.Serialize());

            sw.Stop();
            Log.Debug("RapidCombination Serialize AvatarState: {Elapsed}", sw.Elapsed);
            sw.Restart();


            states = states.SetState(slotAddress, slotState.Serialize());

            sw.Stop();
            Log.Debug("RapidCombination Serialize SlotState: {Elapsed}", sw.Elapsed);

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("RapidCombination Total Executed Time: {Elapsed}", ended - started);

            return states;
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
