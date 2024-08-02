using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.CustomEquipmentCraft;

namespace Nekoyume.Action.CustomEquipmentCraft
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class CustomEquipmentCraft : ActionBase
    {
        public const string TypeIdentifier = "custom_equipment_craft";
        public const int DrawingItemId = 600401;
        public const int DrawingToolItemId = 600402;
        public const int RandomIconId = 0;

        public Address AvatarAddress;
        public int RecipeId;
        public int SlotIndex;
        public int IconId;

        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", TypeIdentifier)
                .Add("values", List.Empty
                    .Add(AvatarAddress.Serialize())
                    .Add(RecipeId)
                    .Add(SlotIndex)
                    .Add(IconId)
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

            // Validate Address
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            if (!Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress))
            {
                throw new InvalidActionFieldException(
                    TypeIdentifier,
                    GetSignerAndOtherAddressesHex(context, AvatarAddress),
                    nameof(AvatarAddress),
                    $"Signer({context.Signer}) is not contained in" +
                    $" AvatarAddress({AvatarAddress}).");
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
                typeof(EquipmentItemOptionSheet),
                typeof(MaterialItemSheet),
                typeof(CustomEquipmentCraftRecipeSheet),
                typeof(CustomEquipmentCraftCostSheet),
                typeof(CustomEquipmentCraftRelationshipSheet),
                typeof(CustomEquipmentCraftIconSheet),
                typeof(CustomEquipmentCraftOptionSheet),
                typeof(CustomEquipmentCraftRecipeSkillSheet),
                typeof(SkillSheet),
                typeof(ArenaSheet),
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

            var relationship = states.GetRelationship(AvatarAddress);
            // Validate Recipe ResultEquipmentId
            var relationshipRow = sheets.GetSheet<CustomEquipmentCraftRelationshipSheet>()
                .OrderedList.First(row => row.Relationship >= relationship);
            var equipmentItemId = relationshipRow.GetItemId(recipeRow.ItemSubType);
            var equipmentItemSheet = sheets.GetSheet<EquipmentItemSheet>();
            if (!equipmentItemSheet.TryGetValue(equipmentItemId, out var equipmentRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(equipmentItemSheet),
                    equipmentItemId
                );
            }
            // ~Validate Recipe ResultEquipmentId

            // Calculate and remove total cost
            var (ncgCost, materialCosts) = CustomCraftHelper.CalculateCraftCost(
                IconId,
                sheets.GetSheet<MaterialItemSheet>(),
                recipeRow,
                relationshipRow,
                sheets.GetSheet<CustomEquipmentCraftCostSheet>().Values
                    .FirstOrDefault(r => r.Relationship == relationship),
                states.GetGameConfigState().CustomEquipmentCraftIconCostMultiplier
            );
            if (ncgCost > 0)
            {
                var arenaData = sheets.GetSheet<ArenaSheet>()
                    .GetRoundByBlockIndex(context.BlockIndex);
                states = states.TransferAsset(context, context.Signer,
                    ArenaHelper.DeriveArenaAddress(arenaData.ChampionshipId, arenaData.Round),
                    ncgCost * states.GetGoldCurrency());
            }

            foreach (var (itemId, amount) in materialCosts)
            {
                if (!avatarState.inventory.RemoveMaterial(itemId, context.BlockIndex, amount))
                {
                    throw new NotEnoughItemException(
                        $"Insufficient material {itemId}: {amount} needed"
                    );
                }
            }

            var random = context.GetRandom();
            var endBlockIndex = context.BlockIndex + (long)Math.Floor(
                recipeRow.RequiredBlock * relationshipRow.RequiredBlockMultiplier / 10000m
            );

            // Create equipment with ItemFactory
            var guid = random.GenerateRandomGuid();
            var equipment =
                (Equipment)ItemFactory.CreateItemUsable(equipmentRow, guid, endBlockIndex);

            // Set Icon
            equipment.IconId = ItemFactory.SelectIconId(
                IconId, IconId == RandomIconId, equipmentRow, relationship,
                sheets.GetSheet<CustomEquipmentCraftIconSheet>(), random
            );

            // Set Elemental Type
            var elementalList = Enum.GetValues<ElementalType>();
            equipment.ElementalType = elementalList[random.Next(0, elementalList.Length)];

            // Set Substats
            var optionRow = ItemFactory.SelectOption(
                recipeRow.ItemSubType, sheets.GetSheet<CustomEquipmentCraftOptionSheet>(), random
            );
            var totalCp = (decimal)random.Next(
                relationshipRow.MinCp,
                relationshipRow.MaxCp + 1
            );

            foreach (var option in optionRow.SubStatData)
            {
                equipment.StatsMap.AddStatAdditionalValue(option.StatType,
                    CPHelper.ConvertCpToStat(option.StatType,
                        totalCp * option.Ratio / optionRow.TotalOptionRatio,
                        avatarState.level)
                );
            }

            // Set skill
            var skill = SkillFactory.SelectSkill(
                recipeRow.ItemSubType,
                sheets.GetSheet<CustomEquipmentCraftRecipeSkillSheet>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                sheets.GetSheet<SkillSheet>(),
                random
            );
            equipment.Skills.Add(skill);

            // Add equipment
            avatarState.inventory.AddItem(equipment);
            avatarState.blockIndex = context.BlockIndex;
            avatarState.updatedAt = context.BlockIndex;

            // Update slot
            var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
            var mailId = random.GenerateRandomGuid();
            var attachmentResult = new CombinationConsumable5.ResultModel
            {
                id = mailId,
                actionPoint = 0,
                gold = ncgCost,
                materials = materialCosts.ToDictionary(
                    e => ItemFactory.CreateMaterial(materialItemSheet, e.Key),
                    e => e.Value),
                itemUsable = equipment,
                recipeId = RecipeId,
                subRecipeId = 0 // This it not required
            };
            slotState.Update(attachmentResult, context.BlockIndex, endBlockIndex);

            // Create mail
            var mail = new CombinationMail(
                attachmentResult,
                context.BlockIndex,
                mailId,
                endBlockIndex);
            avatarState.Update(mail);


            // Add Relationship
            return states
                    .SetLegacyState(slotAddress, slotState.Serialize())
                    .SetAvatarState(AvatarAddress, avatarState)
                    .SetRelationship(AvatarAddress, relationship + 1)
                ;
        }
    }
}
