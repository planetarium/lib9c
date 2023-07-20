using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using Material = Nekoyume.Model.Item.Material;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V200020AccidentObsoleteIndex)]
    [ActionType("combination_consumable7")]
    public class CombinationConsumable7 : GameAction, ICombinationConsumableV1
    {
        public Address AvatarAddress;
        public int recipeId;
        public int slotIndex;

        Address ICombinationConsumableV1.AvatarAddress => AvatarAddress;
        int ICombinationConsumableV1.RecipeId => recipeId;
        int ICombinationConsumableV1.SlotIndex => slotIndex;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var dict = new Dictionary<string, IValue>
                {
                    ["recipeId"] = recipeId.Serialize(),
                    ["avatarAddress"] = AvatarAddress.Serialize(),
                };

                // slotIndex가 포함되지 않은채 나간 버전과 호환을 위해, 0번째 슬롯을 쓰는 경우엔 보내지 않습니다.
                if (slotIndex != 0)
                {
                    dict["slotIndex"] = slotIndex.Serialize();
                }

                return dict.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            recipeId = plainValue["recipeId"].ToInteger();
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            if (plainValue.TryGetValue((Text) "slotIndex", out var value))
            {
                slotIndex = value.ToInteger();
            }
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            IActionContext ctx = context;
            var states = ctx.PreviousState;
            var slotAddress = AvatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );
            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                return states
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Combination exec started", addressesHex);

            if (!states.TryGetAvatarStateV2(ctx.Signer, AvatarAddress, out AvatarState avatarState, out _))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Combination Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!avatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.CombinationEquipmentAction))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction,
                    current);
            }

            var slotState = states.GetCombinationSlotState(AvatarAddress, slotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the slot state is failed to load: # {slotIndex}");
            }

            if(!slotState.Validate(avatarState, ctx.BlockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"{addressesHex}Aborted as the slot state is invalid: {slotState} @ {slotIndex}");
            }

            Log.Verbose("{AddressesHex}Execute Combination; player: {Player}", addressesHex, AvatarAddress);
            var consumableItemSheet = states.GetSheet<ConsumableItemSheet>();
            var recipeRow = states.GetSheet<ConsumableItemRecipeSheet>().Values.FirstOrDefault(r => r.Id == recipeId);
            if (recipeRow is null)
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(ConsumableItemRecipeSheet), recipeId);
            }
            var materials = new Dictionary<Material, int>();
            foreach (var materialInfo in recipeRow.Materials.OrderBy(r => r.Id))
            {
                var materialId = materialInfo.Id;
                var count = materialInfo.Count;
                if (avatarState.inventory.HasItem(materialId, count))
                {
                    avatarState.inventory.TryGetItem(materialId, out var inventoryItem);
                    if (!(inventoryItem.item is Material material))
                    {
                        throw new InvalidMaterialException($"Aborted because material id({materialId}) not valid");
                    }

                    materials[material] = count;
                    avatarState.inventory.RemoveFungibleItem(material, context.BlockIndex, count);
                }
                else
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex}Aborted as the player has no enough material ({materialId} * {count})");
                }
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Combination Remove Materials: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var result = new CombinationConsumable5.ResultModel
            {
                materials = materials,
                itemType = ItemType.Consumable,
            };

            var costAP = recipeRow.RequiredActionPoint;
            if (avatarState.actionPoint < costAP)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: {avatarState.actionPoint} < {costAP}"
                );
            }

            // ap 차감.
            avatarState.actionPoint -= costAP;
            result.actionPoint = costAP;

            var resultConsumableItemId = recipeRow.ResultConsumableItemId;
            sw.Stop();
            Log.Verbose("{AddressesHex}Combination Get Food id: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            result.recipeId = recipeRow.Id;

            if (!consumableItemSheet.TryGetValue(resultConsumableItemId, out var consumableItemRow))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(ConsumableItemSheet), resultConsumableItemId);
            }

            // 조합 결과 획득.
            var requiredBlockIndex = ctx.BlockIndex + recipeRow.RequiredBlockIndex;
            var itemId = ctx.Random.GenerateRandomGuid();
            var itemUsable = GetFood(consumableItemRow, itemId, requiredBlockIndex);
            // 액션 결과
            result.itemUsable = itemUsable;
            var mail = new CombinationMail(
                result,
                ctx.BlockIndex,
                ctx.Random.GenerateRandomGuid(),
                requiredBlockIndex
            );
            result.id = mail.id;
            avatarState.Update(mail);
            avatarState.UpdateFromCombination(itemUsable);
            sw.Stop();
            Log.Verbose("{AddressesHex}Combination Update AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            avatarState.UpdateQuestRewards(materialSheet);

            avatarState.updatedAt = ctx.BlockIndex;
            avatarState.blockIndex = ctx.BlockIndex;
            states = states.SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(inventoryAddress, avatarState.inventory.Serialize())
                    .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                    .SetState(questListAddress, avatarState.questList.Serialize());
            slotState.Update(result, ctx.BlockIndex, requiredBlockIndex);
            sw.Stop();
            Log.Verbose("{AddressesHex}Combination Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Combination Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetState(slotAddress, slotState.Serialize());
        }

        private static ItemUsable GetFood(ConsumableItemSheet.Row equipmentItemRow, Guid itemId, long ctxBlockIndex)
        {
            return ItemFactory.CreateItemUsable(equipmentItemRow, itemId, ctxBlockIndex);
        }
    }
}
