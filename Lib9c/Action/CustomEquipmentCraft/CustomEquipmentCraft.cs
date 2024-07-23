using System;
using System.Collections.Generic;
using System.Globalization;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.CustomEquipmentCraft;
using Serilog;

namespace Nekoyume.Action.CustomEquipmentCraft
{
    [Serializable]
    [ActionType(ActionTypeIdentifier)]
    public class CustomEquipmentCraft : ActionBase
    {
        public const string ActionTypeIdentifier = "custom_equipment_craft";

        public Address AvatarAddress;
        public int RecipeId;
        public int SlotIndex;
        public int IconId;

        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", ActionTypeIdentifier)
                .Add("values", List.Empty
                    .Add(AvatarAddress.Serialize())
                    .Add(RecipeId).Add(SlotIndex).Add(IconId)
                );

        public override void LoadPlainValue(IValue plainValue)
        {
            var lst = (List)((Dictionary)plainValue)["values"];
            AvatarAddress = lst[0].ToAddress();
            RecipeId = (Integer)lst[1];
            SlotIndex = (Integer)lst[2];
            IconId = (Integer)lst[3];
        }


        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var slotAddress = AvatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    SlotIndex
                )
            );

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}CustomEquipmentCraft exec started", addressesHex);
            var agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException(
                    $"[{addressesHex}] Aborted as the agent state of the signer was failed to load.");
            }

            if (!states.TryGetAvatarState(context.Signer, AvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"[{addressesHex}] Aborted as the avatar state of the signer was failed to load.");
            }

            // Validate SlotIndex
            var slotState = states.GetCombinationSlotState(AvatarAddress, SlotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException(
                    $"[{addressesHex}] Aborted as the craft slot state is failed to load: # {SlotIndex}");
            }

            if (!slotState.ValidateV2(avatarState, context.BlockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"[{addressesHex}] Aborted as the craft slot state is invalid: {slotState} @ {SlotIndex}");
            }
            // ~Validate SlotIndex

            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(EquipmentItemSheet),
                typeof(MaterialItemSheet),
                typeof(CustomEquipmentCraftRecipeSheet),
                typeof(CustomEquipmentCraftSkillSheet),
                typeof(CustomEquipmentCraftSubStatSheet),
                typeof(CustomEquipmentCraftProficiencySheet),
                typeof(SkillSheet),
            });

            // Validate RecipeId
            var recipeSheet = sheets.GetSheet<CustomEquipmentCraftRecipeSheet>();
            if (!recipeSheet.TryGetValue(RecipeId, out var recipeRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(EquipmentItemRecipeSheet),
                    RecipeId);
            }
            // ~Validate RecipeId

            // Validate Recipe ResultEquipmentId
            var equipmentItemSheet = sheets.GetSheet<EquipmentItemSheet>();
            if (!equipmentItemSheet.TryGetValue(recipeRow.ResultEquipmentId, out var equipmentRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(equipmentItemSheet),
                    recipeRow.ResultEquipmentId);
            }
            // ~Validate Recipe ResultEquipmentId

            // Validate Materials
            // Should finalize cost using sheet, additional cost and proficiency

            // Remove cost
            // Add proficiency
            var proficiency = states.GetProficiency(AvatarAddress);
            proficiency++;
            states = states.SetProficiency(AvatarAddress, proficiency);
            // Create equipment
            // Set required level
            // Set base stat
            // Set substats
            // Set skill

            // Update slot
            // Create mail

            return states.SetAvatarState(AvatarAddress, avatarState);
        }
    }
}
