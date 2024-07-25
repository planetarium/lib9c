using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions.CustomEquipmentCraft;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
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
                typeof(MaterialItemSheet),
                typeof(CustomEquipmentCraftRecipeSheet),
                typeof(CustomEquipmentCraftCostSheet),
                typeof(CustomEquipmentCraftRelationshipSheet),
                typeof(CustomEquipmentCraftIconSheet),
                typeof(CustomEquipmentCraftOptionSheet),
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

            // Modify cost to get real cost
            var requiredFungibleItems = new Dictionary<int, int>();
            var drawingCost =
                (int)Math.Floor(recipeRow.DrawingAmount * relationshipRow.CostMultiplier);
            var drawingToolCost =
                recipeRow.DrawingToolAmount * relationshipRow.CostMultiplier;
            if (IconId != 0)
            {
                var gameConfig = states.GetGameConfigState();
                drawingToolCost =
                    Math.Floor(drawingToolCost * gameConfig.CustomEquipmentCraftIconCostMultiplier);
            }

            // Remove cost
            if (!avatarState.inventory.RemoveMaterial(DrawingItemId, context.BlockIndex,
                    drawingCost))
            {
                throw new NotEnoughItemException(
                    $"Insufficient material {DrawingItemId}: {drawingCost} needed"
                );
            }

            requiredFungibleItems[DrawingItemId] = drawingCost;

            if (!avatarState.inventory.RemoveMaterial(DrawingToolItemId, context.BlockIndex,
                    (int)drawingToolCost))
            {
                throw new NotEnoughItemException(
                    $"Insufficient material {DrawingItemId}: {drawingToolCost} needed"
                );
            }

            requiredFungibleItems[DrawingToolItemId] = (int)drawingToolCost;

            // Remove additional cost if exists
            var ncgCost = 0L;
            var additionalCostRow = sheets.GetSheet<CustomEquipmentCraftCostSheet>().OrderedList
                .FirstOrDefault(row => row.Relationship == relationship);
            if (additionalCostRow is not null)
            {
                if (additionalCostRow.GoldAmount > 0)
                {
                    ncgCost = (long)additionalCostRow.GoldAmount;
                    var arenaData = sheets.GetSheet<ArenaSheet>()
                        .GetRoundByBlockIndex(context.BlockIndex);
                    states = states.TransferAsset(context, context.Signer,
                        ArenaHelper.DeriveArenaAddress(arenaData.ChampionshipId, arenaData.Round),
                        additionalCostRow.GoldAmount * states.GetGoldCurrency());
                }

                foreach (var materialCost in additionalCostRow.MaterialCosts)
                {
                    if (!avatarState.inventory.RemoveMaterial(materialCost.ItemId,
                            context.BlockIndex,
                            materialCost.Amount))
                    {
                        throw new NotEnoughItemException(
                            $"Insufficient material {materialCost.ItemId}: {materialCost.Amount} needed"
                        );
                    }

                    requiredFungibleItems[materialCost.ItemId] = materialCost.Amount;
                }
            }

            // Create equipment
            var random = context.GetRandom();
            var endBlockIndex = context.BlockIndex + recipeRow.RequiredBlock;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                equipmentRow,
                random.GenerateRandomGuid(),
                endBlockIndex
            );

            // Determine Icon ID
            int iconId;
            var iconSheet = sheets.GetSheet<CustomEquipmentCraftIconSheet>();

            if (IconId == RandomIconId)
            {
                // Random icon
                var iconSelector = new WeightedSelector<CustomEquipmentCraftIconSheet.Row>(random);
                var iconRows = iconSheet.Values
                    .Where(row => row.RequiredRelationship <= relationship);
                foreach (var row in iconRows)
                {
                    iconSelector.Add(row, row.Ratio);
                }

                iconId = iconSelector.Select(1).First().IconId;
            }
            else
            {
                // Selected icon
                var iconRow = iconSheet.Values.FirstOrDefault(row => row.IconId == IconId);
                if (iconRow is null)
                {
                    throw new InvalidActionFieldException($"Icon ID {IconId} is not valid.");
                }

                if (iconRow.RequiredRelationship > relationship)
                {
                    throw new NotEnoughRelationshipException(
                        $"Relationship {relationship} is less than required relationship {iconRow.RequiredRelationship} to use icon {IconId}"
                    );
                }

                if (iconRow.RandomOnly)
                {
                    throw new RandomOnlyIconException(iconRow.IconId);
                }

                iconId = IconId;
            }

            equipment.IconId = iconId;

            // Set substats
            var totalCp = (decimal)random.Next(
                relationshipRow.MinCp,
                relationshipRow.MaxCp + 1
            );
            var optionSelector = new WeightedSelector<CustomEquipmentCraftOptionSheet.Row>(random);
            foreach (var opt in sheets.GetSheet<CustomEquipmentCraftOptionSheet>().Values
                         .Where(row => row.ItemSubType == equipment.ItemSubType))
            {
                optionSelector.Add(opt, opt.Ratio);
            }

            var optionRow = optionSelector.Select(1).First();
            foreach (var option in optionRow.SubStatData)
            {
                equipment.StatsMap.AddStatAdditionalValue(option.StatType,
                    CPHelper.ConvertCpToStat(option.StatType,
                        totalCp * option.Ratio / optionRow.TotalOptionRatio,
                        avatarState.level)
                );
            }

            // Set skill
            var skillSelector =
                new WeightedSelector<CustomEquipmentCraftRecipeSkillSheet.Row>(random);
            foreach (var sr in sheets.GetSheet<CustomEquipmentCraftRecipeSkillSheet>().Values
                         .Where(row => row.ItemSubType == equipment.ItemSubType))
            {
                skillSelector.Add(sr, sr.Ratio);
            }

            var skillId = skillSelector.Select(1).First().SkillId;
            var skillOptionRow = sheets.GetSheet<EquipmentItemOptionSheet>().Values
                .First(row => row.Id == skillId);
            var skillRow = sheets.GetSheet<SkillSheet>().Values
                .First(row => row.Id == skillOptionRow.SkillId);

            var hasStatDamageRatio = skillOptionRow.StatDamageRatioMin != default &&
                                     skillOptionRow.StatDamageRatioMax != default;
            var statDamageRatio = hasStatDamageRatio
                ? random.Next(skillOptionRow.StatDamageRatioMin,
                    skillOptionRow.StatDamageRatioMax + 1)
                : default;
            var refStatType = hasStatDamageRatio
                ? skillOptionRow.ReferencedStatType
                : StatType.NONE;

            var skill = SkillFactory.Get(
                skillRow,
                random.Next(skillOptionRow.SkillDamageMin, skillOptionRow.SkillDamageMax + 1),
                random.Next(skillOptionRow.SkillChanceMin, skillOptionRow.SkillChanceMax + 1),
                statDamageRatio,
                refStatType
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
                materials = requiredFungibleItems.ToDictionary(
                    e => ItemFactory.CreateMaterial(materialItemSheet, e.Key),
                    e => e.Value),
                itemUsable = equipment,
                recipeId = RecipeId,
                subRecipeId = optionRow.Id,
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
                    .SetAvatarState(AvatarAddress, avatarState)
                    .SetRelationship(AvatarAddress, relationship + 1)
                ;
        }
    }
}
