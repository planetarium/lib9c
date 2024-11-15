using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions;
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
    public struct CustomCraftData
    {
        public int RecipeId;
        public int SlotIndex;
        public int IconId;

        public IValue Serialize() => List.Empty.Add(RecipeId).Add(SlotIndex).Add(IconId);
    }

    [Serializable]
    [ActionType(TypeIdentifier)]
    public class CustomEquipmentCraft : ActionBase
    {
        public const string TypeIdentifier = "custom_equipment_craft";
        public const int RandomIconId = 0;

        public Address AvatarAddress;
        public List<CustomCraftData> CraftList;

        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", TypeIdentifier)
                .Add("values", List.Empty
                    .Add(AvatarAddress.Serialize())
                    .Add(new List(CraftList.Select(d => d.Serialize())))
                );

        public override void LoadPlainValue(IValue plainValue)
        {
            var lst = (List)((Dictionary)plainValue)["values"];
            AvatarAddress = lst[0].ToAddress();
            CraftList = new List<CustomCraftData>();
            foreach (var data in (List)lst[1])
            {
                var l = (List)data;
                CraftList.Add(new CustomCraftData
                {
                    RecipeId = (Integer)l[0],
                    SlotIndex = (Integer)l[1],
                    IconId = (Integer)l[2],
                });
            }
        }


        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;

            // Validate duplicated slot indices in action
            var slotIndices = new HashSet<int>(CraftList.Select(c => c.SlotIndex));
            if (slotIndices.Count != CraftList.Count)
            {
                throw new DuplicatedCraftSlotIndexException("Craft slot duplicated.");
            }

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

            var random = context.GetRandom();
            var relationship = states.GetRelationship(AvatarAddress);

            // Create equipment iterating craft data
            foreach (var craftData in CraftList)
            {
                var allSlotState = states.GetAllCombinationSlotState(AvatarAddress);
                if (allSlotState is null)
                {
                    throw new FailedLoadStateException(
                        $"Aborted as the allSlotState was failed to load.");
                }

                // Validate SlotIndex
                var slotState = allSlotState.GetSlot(craftData.SlotIndex);
                if (!slotState.ValidateV2(context.BlockIndex))
                {
                    throw new CombinationSlotUnlockException(
                        $"{addressesHex}Aborted as the slot state is invalid: {slotState} @ {craftData.SlotIndex}");
                }
                // ~Validate SlotIndex

                Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[]
                {
                    typeof(EquipmentItemSheet),
                    typeof(EquipmentItemOptionSheet),
                    typeof(MaterialItemSheet),
                    typeof(CustomEquipmentCraftRecipeSheet),
                    typeof(CustomEquipmentCraftRelationshipSheet),
                    typeof(CustomEquipmentCraftIconSheet),
                    typeof(CustomEquipmentCraftOptionSheet),
                    typeof(CustomEquipmentCraftRecipeSkillSheet),
                    typeof(SkillSheet),
                    typeof(ArenaSheet),
                });

                // Validate RecipeId
                var recipeSheet = sheets.GetSheet<CustomEquipmentCraftRecipeSheet>();
                if (!recipeSheet.TryGetValue(craftData.RecipeId, out var recipeRow))
                {
                    throw new SheetRowNotFoundException(
                        addressesHex,
                        nameof(EquipmentItemRecipeSheet),
                        craftData.RecipeId);
                }
                // ~Validate RecipeId

                // Validate Recipe ResultEquipmentId
                var relationshipSheet = sheets.GetSheet<CustomEquipmentCraftRelationshipSheet>();
                var relationshipRow =
                    relationshipSheet.OrderedList.Last(row => row.Relationship <= relationship);
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
                    craftData.IconId,
                    relationship,
                    sheets.GetSheet<MaterialItemSheet>(),
                    recipeRow,
                    relationshipRow,
                    states.GetGameConfigState().CustomEquipmentCraftIconCostMultiplier
                );

                // Calculate additional costs to move to next group
                var additionalCost =
                    CustomCraftHelper.CalculateAdditionalCost(relationship, relationshipSheet);
                if (additionalCost is not null)
                {
                    ncgCost += additionalCost.Value.Item1;
                    foreach (var cost in additionalCost.Value.Item2)
                    {
                        if (materialCosts.ContainsKey(cost.Key))
                        {
                            materialCosts[cost.Key] += cost.Value;
                        }
                        else
                        {
                            materialCosts[cost.Key] = cost.Value;
                        }
                    }
                }

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

                var endBlockIndex = context.BlockIndex + (long)Math.Floor(
                    recipeRow.RequiredBlock * relationshipRow.RequiredBlockMultiplier / 10000m
                );

                // Create equipment with ItemFactory
                var guid = random.GenerateRandomGuid();
                var equipment =
                    (Equipment)ItemFactory.CreateItemUsable(equipmentRow, guid, endBlockIndex);
                equipment.ByCustomCraft = true;

                // Set Icon
                equipment.CraftWithRandom = craftData.IconId == RandomIconId;

                var (iconId, isRandomOnlyIcon) = ItemFactory.SelectIconId(
                    craftData.IconId, craftData.IconId == RandomIconId, equipmentRow, relationship,
                    sheets.GetSheet<CustomEquipmentCraftIconSheet>(), random
                );
                equipment.IconId = iconId;
                equipment.HasRandomOnlyIcon = isRandomOnlyIcon;

                // Set Elemental Type
                var elementalList = (ElementalType[])Enum.GetValues(typeof(ElementalType));
                equipment.ElementalType = elementalList[random.Next(elementalList.Length)];

                // Set Substats
                var totalCp = (decimal)CustomCraftHelper.SelectCp(relationshipRow, random);
                var optionRow = ItemFactory.SelectOption(
                    recipeRow.ItemSubType, sheets.GetSheet<CustomEquipmentCraftOptionSheet>(),
                    random
                );

                foreach (var option in optionRow.SubStatData)
                {
                    equipment.StatsMap.AddStatAdditionalValue(option.StatType,
                        CPHelper.ConvertCpToStat(option.StatType,
                            totalCp * option.Ratio / optionRow.TotalOptionRatio,
                            avatarState.level)
                    );
                    equipment.optionCountFromCombination++;
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
                equipment.optionCountFromCombination++;

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
                    recipeId = craftData.RecipeId,
                    subRecipeId = 0 // This it not required
                };
                slotState.Update(attachmentResult, context.BlockIndex, endBlockIndex);
                allSlotState.SetSlot(slotState);

                // Create mail
                var mail = new CustomCraftMail(
                    context.BlockIndex,
                    mailId,
                    endBlockIndex,
                    equipment);
                avatarState.Update(mail);

                relationship++;
                states = states.SetCombinationSlotState(AvatarAddress, allSlotState);
            }

            // Add Relationship
            return states
                .SetAvatarState(AvatarAddress, avatarState)
                .SetRelationship(AvatarAddress, relationship);
        }
    }
}
