using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;
using Nekoyume.Action.Results;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Pet;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1711
    /// </summary>
    [Serializable]
    [ActionType("rapid_combination9")]
    public class RapidCombination : GameAction, IRapidCombinationV1
    {
        public Address avatarAddress;
        public int slotIndex;

        Address IRapidCombinationV1.AvatarAddress => avatarAddress;
        int IRapidCombinationV1.SlotIndex => slotIndex;

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var world = context.PreviousState;
            var slotAddress = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                world = LegacyModule.SetState(world, avatarAddress, MarkChanged);
                world = LegacyModule.SetState(world, inventoryAddress, MarkChanged);
                world = LegacyModule.SetState(world, worldInformationAddress, MarkChanged);
                world = LegacyModule.SetState(world, questListAddress, MarkChanged);
                world = LegacyModule.SetState(world, slotAddress, MarkChanged);
                return world;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RapidCombination exec started", addressesHex);

            if (!AvatarModule.TryGetAgentAvatarStatesV2(
                    world,
                    context.Signer,
                    avatarAddress,
                    out var agentState,
                    out var avatarState,
                    out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var slotState = LegacyModule.GetCombinationSlotState(world, avatarAddress, slotIndex);
            if (slotState?.Result is null)
            {
                throw new CombinationSlotResultNullException($"{addressesHex}CombinationSlot Result is null. ({avatarAddress}), ({slotIndex})");
            }

            if(!avatarState.worldInformation.IsStageCleared(slotState.UnlockStage))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(addressesHex, slotState.UnlockStage, current);
            }

            var diff = slotState.Result.itemUsable.RequiredBlockIndex - context.BlockIndex;
            if (diff <= 0)
            {
                throw new RequiredBlockIndexException($"{addressesHex}Already met the required block index. context block index: {context.BlockIndex}, required block index: {slotState.Result.itemUsable.RequiredBlockIndex}");
            }

            var gameConfigState = LegacyModule.GetGameConfigState(world);
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the GameConfigState was failed to load.");
            }

            var actionableBlockIndex = slotState.StartBlockIndex +
                                       LegacyModule.GetGameConfigState(world).RequiredAppraiseBlock;
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
                if (!LegacyModule.TryGetState(world, petStateAddress, out List rawState))
                {
                    throw new FailedLoadStateException($"{addressesHex}Aborted as the {nameof(PetState)} was failed to load.");
                }

                petState = new PetState(rawState);
                var petOptionSheet = LegacyModule.GetSheet<PetOptionSheet>(world);
                costHourglassCount = PetHelper.CalculateDiscountedHourglass(
                    diff,
                    gameConfigState.HourglassPerBlock,
                    petState,
                    petOptionSheet);
            }
            else
            {
                costHourglassCount = Statics.RapidCombination.CalculateHourglassCountV0(gameConfigState, diff);
            }

            var materialItemSheet = LegacyModule.GetSheet<MaterialItemSheet>(world);
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
            avatarState.UpdateFromRapidCombinationV2(
                (RapidCombination5Result)slotState.Result,
                context.BlockIndex);

            // Update Pet
            if (!(petState is null))
            {
                petState.Update(context.BlockIndex);
                var petStateAddress = PetState.DeriveAddress(avatarAddress, petState.PetId);
                world = LegacyModule.SetState(world, petStateAddress, petState.Serialize());
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RapidCombination Total Executed Time: {Elapsed}", addressesHex, ended - started);
            world = AvatarModule.SetAvatarStateV2(world, avatarAddress, avatarState);
            world = LegacyModule.SetState(
                world,
                inventoryAddress,
                avatarState.inventory.Serialize());
            world = LegacyModule.SetState(
                world,
                worldInformationAddress,
                avatarState.worldInformation.Serialize());
            world = LegacyModule.SetState(
                world,
                questListAddress,
                avatarState.questList.Serialize());
            world = LegacyModule.SetState(world, slotAddress, slotState.Serialize());
            return world;
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
