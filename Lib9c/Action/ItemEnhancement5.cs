using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("item_enhancement5")]
    public class ItemEnhancement5 : GameAction
    {
        public const int RequiredBlockCount = 1;

        public static readonly Address BlacksmithAddress = Addresses.Blacksmith;

        public Guid itemId;
        public Guid materialId;
        public Address avatarAddress;
        public int slotIndex;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var slotAddress = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );
            if (ctx.Rehearsal)
            {
                return states
                    .MarkBalanceChanged(GoldCurrencyMock, ctx.Signer, BlacksmithAddress)
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}ItemEnhancement exec started", addressesHex);

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState agentState,
                out AvatarState avatarState))
            {
                var exc = new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
                Log.Error(exc.Message);
                throw exc;
            }
            sw.Stop();
            Log.Debug("{AddressesHex}ItemEnhancement Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable enhancementItem))
            {
                var exc = new ItemDoesNotExistException(
                    $"{addressesHex}Aborted as the NonFungibleItem ({itemId}) was failed to load from avatar's inventory."
                );
                Log.Error(exc.Message);
                throw exc;
            }

            if (enhancementItem.RequiredBlockIndex > context.BlockIndex)
            {
                var exc = new RequiredBlockIndexException(
                    $"{addressesHex}Aborted as the equipment to enhance ({itemId}) is not available yet; it will be available at the block #{enhancementItem.RequiredBlockIndex}."
                );
                Log.Error(exc.Message);
                throw exc;
            }

            if (!(enhancementItem is Equipment enhancementEquipment))
            {
                var exc = new InvalidCastException(
                    $"{addressesHex}Aborted as the item is not a {nameof(Equipment)}, but {enhancementItem.GetType().Name}."
                );
                Log.Error(exc.Message);
                throw exc;
            }

            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState is null)
            {
                var exc = new FailedLoadStateException($"{addressesHex}Aborted as the slot state was failed to load. #{slotIndex}");
                Log.Error(exc.Message);
                throw exc;
            }

            if (!slotState.Validate(avatarState, ctx.BlockIndex))
            {
                var exc = new CombinationSlotUnlockException($"{addressesHex}Aborted as the slot state was failed to invalid. #{slotIndex}");
                Log.Error(exc.Message);
                throw exc;
            }

            sw.Stop();
            Log.Debug("{AddressesHex}ItemEnhancement Get Equipment: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (enhancementEquipment.level > 9)
            {
                // Maximum level exceeded.
                var exc = new EquipmentLevelExceededException(
                    $"{addressesHex}Aborted due to invalid equipment level: {enhancementEquipment.level} < 9"
                );
                Log.Error(exc.Message);
                throw exc;
            }

            var result = new ItemEnhancement.ResultModel
            {
                itemUsable = enhancementEquipment,
                materialItemIdList = new[] { materialId }
            };

            var requiredAP = ItemEnhancement.GetRequiredAp();
            if (avatarState.actionPoint < requiredAP)
            {
                var exc = new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: {avatarState.actionPoint} < {requiredAP}"
                );
                Log.Error(exc.Message);
                throw exc;
            }

            var enhancementCostSheet = states.GetSheet<EnhancementCostSheet>();
            var requiredNCG = ItemEnhancement.GetRequiredNCG(enhancementCostSheet, enhancementEquipment.Grade, enhancementEquipment.level + 1);

            avatarState.actionPoint -= requiredAP;
            result.actionPoint = requiredAP;

            if (requiredNCG > 0)
            {
                states = states.TransferAsset(
                    ctx.Signer,
                    BlacksmithAddress,
                    states.GetGoldCurrency() * requiredNCG
                );
            }

            if (!avatarState.inventory.TryGetNonFungibleItem(materialId, out ItemUsable materialItem))
            {
                var exc = new NotEnoughMaterialException(
                    $"{addressesHex}Aborted as the signer does not have a necessary material ({materialId})."
                );
                Log.Error(exc.Message);
                throw exc;
            }

            if (materialItem.RequiredBlockIndex > context.BlockIndex)
            {
                var exc = new RequiredBlockIndexException(
                    $"{addressesHex}Aborted as the material ({materialId}) is not available yet; it will be available at the block #{materialItem.RequiredBlockIndex}."
                );
                Log.Error(exc.Message);
                throw exc;
            }

            if (!(materialItem is Equipment materialEquipment))
            {
                var exc = new InvalidCastException(
                    $"{addressesHex}Aborted as the material item is not an {nameof(Equipment)}, but {materialItem.GetType().Name}."
                );
                Log.Error(exc.Message);
                throw exc;
            }

            if (enhancementEquipment.ItemId == materialId)
            {
                var exc = new InvalidMaterialException(
                    $"{addressesHex}Aborted as an equipment to enhance ({materialId}) was used as a material too."
                );
                Log.Error(exc.Message);
                throw exc;
            }

            if (materialEquipment.ItemSubType != enhancementEquipment.ItemSubType)
            {
                // Invalid ItemSubType
                var exc = new InvalidMaterialException(
                    $"{addressesHex}Aborted as the material item is not a {enhancementEquipment.ItemSubType}, but {materialEquipment.ItemSubType}."
                );
                Log.Error(exc.Message);
                throw exc;
            }

            if (materialEquipment.Grade != enhancementEquipment.Grade)
            {
                // Invalid Grade
                var exc = new InvalidMaterialException(
                    $"{addressesHex}Aborted as grades of the equipment to enhance ({enhancementEquipment.Grade}) and a material ({materialEquipment.Grade}) does not match."
                );
                Log.Error(exc.Message);
                throw exc;
            }

            if (materialEquipment.level != enhancementEquipment.level)
            {
                // Invalid level
                var exc = new InvalidMaterialException(
                    $"{addressesHex}Aborted as levels of the equipment to enhance ({enhancementEquipment.level}) and a material ({materialEquipment.level}) does not match."
                );
                Log.Error(exc.Message);
                throw exc;
            }
            sw.Stop();
            Log.Debug("{AddressesHex}ItemEnhancement Get Material: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            materialEquipment.Unequip();

            enhancementEquipment.Unequip();

            enhancementEquipment = ItemEnhancement.UpgradeEquipment(enhancementEquipment);

            var requiredBlockIndex = ctx.BlockIndex + RequiredBlockCount;
            enhancementEquipment.Update(requiredBlockIndex);
            sw.Stop();
            Log.Debug("{AddressesHex}ItemEnhancement Upgrade Equipment: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            result.gold = requiredNCG;

            avatarState.inventory.RemoveNonFungibleItem(materialId);
            sw.Stop();
            Log.Debug("{AddressesHex}ItemEnhancement Remove Materials: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            var mail = new ItemEnhanceMail(result, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(), requiredBlockIndex);
            result.id = mail.id;

            avatarState.inventory.RemoveNonFungibleItem(enhancementEquipment);
            avatarState.UpdateV3(mail);
            avatarState.UpdateFromItemEnhancement(enhancementEquipment);

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            avatarState.UpdateQuestRewards(materialSheet);

            slotState.Update(result, ctx.BlockIndex, requiredBlockIndex);

            sw.Stop();
            Log.Debug("{AddressesHex}ItemEnhancement Update AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            states = states.SetState(avatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Debug("{AddressesHex}ItemEnhancement Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}ItemEnhancement Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states.SetState(slotAddress, slotState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var dict = new Dictionary<string, IValue>
                {
                    ["itemId"] = itemId.Serialize(),
                    ["materialId"] = materialId.Serialize(),
                    ["avatarAddress"] = avatarAddress.Serialize(),
                    ["slotIndex"] = slotIndex.Serialize(),
                };

                return dict.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            itemId = plainValue["itemId"].ToGuid();
            materialId = plainValue["materialId"].ToGuid();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            slotIndex = plainValue["slotIndex"].ToInteger();
        }
    }
}
