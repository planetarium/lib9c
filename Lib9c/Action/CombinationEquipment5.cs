using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V200020AccidentObsoleteIndex)]
    [ActionType("combination_equipment5")]
    public class CombinationEquipment5 : GameAction, ICombinationEquipmentV1
    {
        public static readonly Address BlacksmithAddress = ItemEnhancement9.BlacksmithAddress;

        public Address AvatarAddress;
        public int RecipeId;
        public int SlotIndex;
        public int? SubRecipeId;

        Address ICombinationEquipmentV1.AvatarAddress => AvatarAddress;
        int ICombinationEquipmentV1.RecipeId => RecipeId;
        int ICombinationEquipmentV1.SlotIndex => SlotIndex;
        int? ICombinationEquipmentV1.SubRecipeId => SubRecipeId;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            IActionContext ctx = context;
            var states = ctx.PreviousState;
            var slotAddress = AvatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    SlotIndex
                )
            );
            if (ctx.Rehearsal)
            {
                return states
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged)
                    .MarkBalanceChanged(ctx, GoldCurrencyMock, ctx.Signer, BlacksmithAddress);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);

            if (!states.TryGetAgentAvatarStates(ctx.Signer, AvatarAddress, out var agentState,
                out var avatarState))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var slotState = states.GetCombinationSlotState(AvatarAddress, SlotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the slot state is failed to load");
            }

            if (!slotState.Validate(avatarState, ctx.BlockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"{addressesHex}Aborted as the slot state is invalid: {slotState} @ {SlotIndex}");
            }

            var recipeSheet = states.GetSheet<EquipmentItemRecipeSheet>();
            var materialSheet = states.GetSheet<MaterialItemSheet>();
            var materials = new Dictionary<Material, int>();

            // Validate recipe.
            if (!recipeSheet.TryGetValue(RecipeId, out var recipe))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(EquipmentItemRecipeSheet), RecipeId);
            }

            if (!(SubRecipeId is null))
            {
                if (!recipe.SubRecipeIds.Contains((int) SubRecipeId))
                {
                    throw new SheetRowColumnException(
                        $"{addressesHex}Aborted as the sub recipe {SubRecipeId} was failed to load from the sheet."
                    );
                }
            }

            // Validate main recipe is unlocked.
            if (!avatarState.worldInformation.IsStageCleared(recipe.UnlockStage))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(addressesHex, recipe.UnlockStage, current);
            }

            if (!materialSheet.TryGetValue(recipe.MaterialId, out var material))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(MaterialItemSheet), recipe.MaterialId);
            }

            if (!avatarState.inventory.RemoveFungibleItem2(material.ItemId, recipe.MaterialCount))
            {
                throw new NotEnoughMaterialException(
                    $"{addressesHex}Aborted as the player has no enough material ({material} * {recipe.MaterialCount})"
                );
            }

            var equipmentMaterial = ItemFactory.CreateMaterial(materialSheet, material.Id);
            materials[equipmentMaterial] = recipe.MaterialCount;

            BigInteger requiredGold = recipe.RequiredGold;
            var requiredActionPoint = recipe.RequiredActionPoint;
            var equipmentItemSheet = states.GetSheet<EquipmentItemSheet>();

            // Validate equipment id.
            if (!equipmentItemSheet.TryGetValue(recipe.ResultEquipmentId, out var equipRow))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(equipmentItemSheet), recipe.ResultEquipmentId);
            }

            var requiredBlockIndex = ctx.BlockIndex + recipe.RequiredBlockIndex;
            var equipment = (Equipment) ItemFactory.CreateItemUsable(
                equipRow,
                ctx.Random.GenerateRandomGuid(),
                requiredBlockIndex
            );

            // Validate sub recipe.
            HashSet<int> optionIds = null;
            if (SubRecipeId.HasValue)
            {
                var subSheet = states.GetSheet<EquipmentItemSubRecipeSheet>();
                var subId = (int) SubRecipeId;
                if (!subSheet.TryGetValue(subId, out var subRecipe))
                {
                    throw new SheetRowNotFoundException(addressesHex, nameof(EquipmentItemSubRecipeSheet), subId);
                }

                requiredBlockIndex += subRecipe.RequiredBlockIndex;
                requiredGold += subRecipe.RequiredGold;
                requiredActionPoint += subRecipe.RequiredActionPoint;

                foreach (var materialInfo in subRecipe.Materials)
                {
                    if (!materialSheet.TryGetValue(materialInfo.Id, out var subMaterialRow))
                    {
                        throw new SheetRowNotFoundException(addressesHex, nameof(MaterialItemSheet), materialInfo.Id);
                    }

                    if (!avatarState.inventory.RemoveFungibleItem2(subMaterialRow.ItemId,
                        materialInfo.Count))
                    {
                        throw new NotEnoughMaterialException(
                            $"{addressesHex}Aborted as the player has no enough material ({subMaterialRow} * {materialInfo.Count})"
                        );
                    }

                    var subMaterial = ItemFactory.CreateMaterial(materialSheet, materialInfo.Id);
                    materials[subMaterial] = materialInfo.Count;
                }

                optionIds = SelectOption(states.GetSheet<EquipmentItemOptionSheet>(), states.GetSheet<SkillSheet>(),
                    subRecipe, ctx.Random, equipment);
                equipment.Update(requiredBlockIndex);
            }

            // Validate NCG.
            FungibleAssetValue agentBalance = states.GetBalance(ctx.Signer, states.GetGoldCurrency());
            if (agentBalance < states.GetGoldCurrency() * requiredGold)
            {
                throw new InsufficientBalanceException(
                    $"{addressesHex}Aborted as the agent ({ctx.Signer}) has no sufficient gold: {agentBalance} < {requiredGold}",
                    ctx.Signer,
                    agentBalance
                );
            }

            if (avatarState.actionPoint < requiredActionPoint)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: {avatarState.actionPoint} < {requiredActionPoint}"
                );
            }

            avatarState.actionPoint -= requiredActionPoint;
            if (!(optionIds is null))
            {
                foreach (var id in optionIds.OrderBy(id => id))
                {
                    agentState.unlockedOptions.Add(id);
                }
            }

            // FIXME: BlacksmithAddress just accumulate NCG. we need plan how to circulate this.
            if (requiredGold > 0)
            {
                states = states.TransferAsset(
                    ctx,
                    ctx.Signer,
                    BlacksmithAddress,
                    states.GetGoldCurrency() * requiredGold
                );
            }

            var result = new CombinationConsumable5.ResultModel
            {
                actionPoint = requiredActionPoint,
                gold = requiredGold,
                materials = materials,
                itemUsable = equipment,
                recipeId = RecipeId,
                subRecipeId = SubRecipeId,
                itemType = ItemType.Equipment,
            };
            slotState.Update(result, ctx.BlockIndex, requiredBlockIndex);
            var mail = new CombinationMail(result, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(),
                requiredBlockIndex);
            result.id = mail.id;
            avatarState.Update(mail);
            avatarState.questList.UpdateCombinationEquipmentQuest(RecipeId);
            avatarState.UpdateFromCombination2(equipment);
            avatarState.UpdateQuestRewards2(materialSheet);
            return states
                .SetState(AvatarAddress, avatarState.Serialize())
                .SetState(slotAddress, slotState.Serialize())
                .SetState(ctx.Signer, agentState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["recipeId"] = RecipeId.Serialize(),
                ["subRecipeId"] = SubRecipeId.Serialize(),
                ["slotIndex"] = SlotIndex.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            RecipeId = plainValue["recipeId"].ToInteger();
            SubRecipeId = plainValue["subRecipeId"].ToNullableInteger();
            SlotIndex = plainValue["slotIndex"].ToInteger();
        }

        public static DecimalStat GetStat(EquipmentItemOptionSheet.Row row, IRandom random)
        {
            var value = random.Next(row.StatMin, row.StatMax + 1);
            return new DecimalStat(row.StatType, value);
        }

        public static Skill GetSkill(EquipmentItemOptionSheet.Row row, SkillSheet skillSheet,
            IRandom random)
        {
            try
            {
                var skillRow = skillSheet.OrderedList.First(r => r.Id == row.SkillId);
                var dmg = random.Next(row.SkillDamageMin, row.SkillDamageMax + 1);
                var chance = random.Next(row.SkillChanceMin, row.SkillChanceMax + 1);
                var skill = SkillFactory.GetV1(skillRow, dmg, chance);
                return skill;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public static HashSet<int> SelectOption(
            EquipmentItemOptionSheet optionSheet,
            SkillSheet skillSheet,
            EquipmentItemSubRecipeSheet.Row subRecipe,
            IRandom random,
            Equipment equipment
        )
        {
            var optionSelector = new WeightedSelector<EquipmentItemOptionSheet.Row>(random);
            var optionIds = new HashSet<int>();

            // Skip sort subRecipe.Options because it had been already sorted in WeightedSelector.Select();
            foreach (var optionInfo in subRecipe.Options)
            {
                if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                {
                    continue;
                }

                optionSelector.Add(optionRow, optionInfo.Ratio);
            }

            IEnumerable<EquipmentItemOptionSheet.Row> optionRows =
                new EquipmentItemOptionSheet.Row[0];
            try
            {
                optionRows = optionSelector.SelectV1(subRecipe.MaxOptionLimit);
            }
            catch (Exception e) when (
                e is InvalidCountException ||
                e is ListEmptyException
            )
            {
                return optionIds;
            }
            finally
            {
                foreach (var optionRow in optionRows.OrderBy(r => r.Id))
                {
                    if (optionRow.StatType != StatType.NONE)
                    {
                        var stat = GetStat(optionRow, random);
                        equipment.StatsMap.AddStatAdditionalValue(stat.StatType, stat.BaseValue);
                    }
                    else
                    {
                        var skill = GetSkill(optionRow, skillSheet, random);
                        if (!(skill is null))
                        {
                            equipment.Skills.Add(skill);
                        }
                    }

                    optionIds.Add(optionRow.Id);
                }
            }

            return optionIds;
        }
    }
}
