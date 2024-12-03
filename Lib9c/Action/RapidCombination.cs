using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Pet;
using Serilog;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2195
    /// </summary>
    [Serializable]
    [ActionType("rapid_combination10")]
    public class RapidCombination : GameAction, IRapidCombinationV2
    {
        public Address avatarAddress;
        public List<int> slotIndexList = new();

        Address IRapidCombinationV2.AvatarAddress => avatarAddress;
        List<int> IRapidCombinationV2.SlotIndexList => slotIndexList;

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RapidCombination exec started", addressesHex);

            var agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var avatarState = states.GetAvatarState(avatarAddress, true, false, false);
            if (avatarState is null || !avatarState.agentAddress.Equals(context.Signer))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var allSlotState = states.GetAllCombinationSlotState(avatarAddress);
            if (allSlotState is null)
            {
                throw new FailedLoadStateException($"Aborted as the allSlotState was failed to load.");
            }

            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(PetOptionSheet),
                    typeof(MaterialItemSheet),
                });

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the GameConfigState was failed to load.");
            }

            void ProcessRapidCombination(int si)
            {
                var slotState = allSlotState.GetSlot(si);
                if (slotState.Result is null)
                {
                    throw new CombinationSlotResultNullException($"{addressesHex}CombinationSlot Result is null. ({avatarAddress}), ({si})");
                }

                var diff = slotState.Result.itemUsable.RequiredBlockIndex - context.BlockIndex;
                if (diff <= 0)
                {
                    throw new RequiredBlockIndexException($"{addressesHex}Already met the required block index. context block index: {context.BlockIndex}, required block index: {slotState.Result.itemUsable.RequiredBlockIndex}");
                }

                var actionableBlockIndex = slotState.WorkStartBlockIndex;
                if (context.BlockIndex < actionableBlockIndex)
                {
                    throw new AppraiseBlockNotReachedException(
                        $"{addressesHex}Aborted as Item appraisal block section. " +
                        $"context block index: {context.BlockIndex}, actionable block index : {actionableBlockIndex}");
                }

                int costHourglassCount = 0;
                PetState petState = null;
                if (slotState.PetId.HasValue)
                {
                    var petStateAddress = PetState.DeriveAddress(avatarAddress, slotState.PetId.Value);
                    if (!states.TryGetLegacyState(petStateAddress, out List rawState))
                    {
                        throw new FailedLoadStateException($"{addressesHex}Aborted as the {nameof(PetState)} was failed to load.");
                    }

                    petState = new PetState(rawState);
                    var petOptionSheet = sheets.GetSheet<PetOptionSheet>();
                    costHourglassCount = PetHelper.CalculateDiscountedHourglass(
                        diff,
                        gameConfigState.HourglassPerBlock,
                        petState,
                        petOptionSheet);
                }
                else
                {
                    costHourglassCount = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
                }

                var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
                var row = materialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Hourglass);
                var hourGlass = ItemFactory.CreateMaterial(row);
                if (!avatarState.inventory.RemoveFungibleItem(hourGlass, context.BlockIndex, costHourglassCount))
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex}Aborted as the player has no enough material ({row.Id} * {costHourglassCount})");
                }

                if (slotState.TryGetResultId(out var resultId) &&
                    avatarState.mailBox.All(mail => mail.id != resultId) &&
                    slotState.TryGetMail(
                        context.BlockIndex,
                        context.BlockIndex,
                        out var combinationMail,
                        out var itemEnhanceMail))
                {
                    if (combinationMail != null)
                    {
                        avatarState.Update(combinationMail);
                    }
                    else if (itemEnhanceMail != null)
                    {
                        avatarState.Update(itemEnhanceMail);
                    }
                }

                slotState.UpdateV2(context.BlockIndex, hourGlass, costHourglassCount);
                allSlotState.SetSlot(slotState);
                avatarState.UpdateFromRapidCombinationV2(
                    (RapidCombination5.ResultModel)slotState.Result,
                    context.BlockIndex);

                // Update Pet
                if (petState is not null)
                {
                    petState.Update(context.BlockIndex);
                    var petStateAddress = PetState.DeriveAddress(avatarAddress, petState.PetId);
                    states = states.SetLegacyState(petStateAddress, petState.Serialize());
                }
            }

            foreach (var slotIndex in slotIndexList.Distinct())
            {
                ProcessRapidCombination(slotIndex);
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RapidCombination Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetAvatarState(avatarAddress, avatarState, true, true, false, false)
                .SetCombinationSlotState(avatarAddress, allSlotState);
        }

#region Serialize
        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = avatarAddress.Serialize(),
                ["s"] = new List(slotIndexList.OrderBy(i => i).Select(i => (Integer)i)),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["a"].ToAddress();
            slotIndexList = plainValue["s"].ToList(i => (int)(Integer)i);
        }
#endregion Serialize
    }
}
