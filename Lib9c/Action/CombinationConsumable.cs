#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Lib9c.Model.Item;
using Lib9c.Model.Mail;
using Lib9c.Model.State;
using Lib9c.Module;
using Lib9c.TableData;
using Lib9c.TableData.Item;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Serilog;

namespace Lib9c.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2195
    /// </summary>
    [Serializable]
    [ActionType("combination_consumable9")]
    public class CombinationConsumable : GameAction, ICombinationConsumableV1
    {
        public const string AvatarAddressKey = "a";
        public Address avatarAddress;

        public const string SlotIndexKey = "s";
        public int slotIndex;

        public const string RecipeIdKey = "r";
        public int recipeId;

        Address ICombinationConsumableV1.AvatarAddress => avatarAddress;
        int ICombinationConsumableV1.RecipeId => recipeId;
        int ICombinationConsumableV1.SlotIndex => slotIndex;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [AvatarAddressKey] = avatarAddress.Serialize(),
                [SlotIndexKey] = slotIndex.Serialize(),
                [RecipeIdKey] = recipeId.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue[AvatarAddressKey].ToAddress();
            slotIndex = plainValue[SlotIndexKey].ToInteger();
            recipeId = plainValue[RecipeIdKey].ToInteger();
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Combination exec started", addressesHex);

            if (!states.TryGetAvatarState(context.Signer, avatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var allSlotState = states.GetAllCombinationSlotState(avatarAddress);
            if (allSlotState is null)
            {
                throw new FailedLoadStateException($"Aborted as the allSlotState was failed to load.");
            }

            // Validate SlotIndex
            var slotState = allSlotState.GetSlot(slotIndex);
            if (!slotState.ValidateV2(context.BlockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"{addressesHex}Aborted as the slot state is invalid: {slotState} @ {slotIndex}");
            }
            // ~Validate SlotIndex

            // Validate Work
            var costActionPoint = 0;
            var endBlockIndex = context.BlockIndex;
            var requiredFungibleItems = new Dictionary<int, int>();

            // Validate RecipeId
            var consumableItemRecipeSheet = states.GetSheet<ConsumableItemRecipeSheet>();
            if (!consumableItemRecipeSheet.TryGetValue(recipeId, out var recipeRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(ConsumableItemRecipeSheet),
                    recipeId);
            }
            // ~Validate RecipeId

            // Validate Recipe ResultEquipmentId
            var consumableItemSheet = states.GetSheet<ConsumableItemSheet>();
            if (!consumableItemSheet.TryGetValue(recipeRow.ResultConsumableItemId, out var consumableRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(consumableItemSheet),
                    recipeRow.ResultConsumableItemId);
            }
            // ~Validate Recipe ResultEquipmentId

            // Validate Recipe Material
            var materialItemSheet = states.GetSheet<MaterialItemSheet>();
            for (var i = recipeRow.Materials.Count; i > 0; i--)
            {
                var materialInfo = recipeRow.Materials[i - 1];
                if (!materialItemSheet.TryGetValue(materialInfo.Id, out var materialRow))
                {
                    throw new SheetRowNotFoundException(
                        addressesHex,
                        nameof(MaterialItemSheet),
                        materialInfo.Id);
                }

                if (requiredFungibleItems.ContainsKey(materialRow.Id))
                {
                    requiredFungibleItems[materialRow.Id] += materialInfo.Count;
                }
                else
                {
                    requiredFungibleItems[materialRow.Id] = materialInfo.Count;
                }
            }
            // ~Validate Recipe Material

            costActionPoint += recipeRow.RequiredActionPoint;
            endBlockIndex += recipeRow.RequiredBlockIndex;
            // ~Validate Work

            // Remove Required Materials
            foreach (var pair in requiredFungibleItems.OrderBy(pair => pair.Key))
            {
                if (!materialItemSheet.TryGetValue(pair.Key, out var materialRow) ||
                    !avatarState.inventory.RemoveFungibleItem(materialRow.ItemId, context.BlockIndex, pair.Value))
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex}Aborted as the player has no enough material ({pair.Key} * {pair.Value})");
                }
            }
            // ~Remove Required Materials

            // Subtract Required ActionPoint
            // 2024-03-29 기준: 레시피에 CostActionPoint가 포함된 케이스는 없으나 TableSheets 상태에 의해 동작이 변경될 수 있기에 작성해둔다.
            if (costActionPoint > 0)
            {
                if (!states.TryGetActionPoint(avatarAddress, out var actionPoint))
                {
                    actionPoint = avatarState.actionPoint;
                }

                if (actionPoint < costActionPoint)
                {
                    throw new NotEnoughActionPointException(
                        $"{addressesHex}Aborted due to insufficient action point: {actionPoint} < {costActionPoint}"
                    );
                }

                actionPoint -= costActionPoint;
                states = states.SetActionPoint(avatarAddress, actionPoint);
            }
            // ~Subtract Required ActionPoint

            // Create Consumable
            var random = context.GetRandom();
            var consumable = (Consumable) ItemFactory.CreateItemUsable(
                consumableRow,
                random.GenerateRandomGuid(),
                endBlockIndex
            );
            // ~Create Consumable

            // Add or Update Consumable
            avatarState.blockIndex = context.BlockIndex;
            avatarState.updatedAt = context.BlockIndex;
            avatarState.UpdateFromCombination(consumable);
            avatarState.UpdateQuestRewards(materialItemSheet);
            // ~Add or Update Consumable

            // Update Slot
            var mailId = random.GenerateRandomGuid();
            var attachmentResult = new CombinationConsumable5.ResultModel
            {
                id = mailId,
                actionPoint = costActionPoint,
                materials = requiredFungibleItems.ToDictionary(
                    e => ItemFactory.CreateMaterial(materialItemSheet, e.Key),
                    e => e.Value),
                itemUsable = consumable,
                recipeId = recipeId,
            };
            slotState.Update(attachmentResult, context.BlockIndex, endBlockIndex);
            allSlotState.SetSlot(slotState);
            // ~Update Slot

            // Create Mail
            var mail = new CombinationMail(
                attachmentResult,
                context.BlockIndex,
                mailId,
                endBlockIndex);
            avatarState.Update(mail);
            // ~Create Mail

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Combination Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states
                .SetAvatarState(avatarAddress, avatarState)
                .SetCombinationSlotState(avatarAddress, allSlotState);
        }
    }
}
