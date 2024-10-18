using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions;
using Nekoyume.Arena;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Summon;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("aura_summon")]
    public class AuraSummon : GameAction, IAuraSummonV1
    {
        public const string AvatarAddressKey = "aa";
        public Address AvatarAddress;

        public const string GroupIdKey = "gid";
        public int GroupId;

        public const string SummonCountKey = "sc";
        public int SummonCount;

        Address IAuraSummonV1.AvatarAddress => AvatarAddress;
        int IAuraSummonV1.GroupId => GroupId;
        int IAuraSummonV1.SummonCount => SummonCount;

        public AuraSummon()
        {
        }

        public AuraSummon(Address avatarAddress, int groupId, int summonCount)
        {
            AvatarAddress = avatarAddress;
            GroupId = groupId;
            SummonCount = summonCount;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [AvatarAddressKey] = AvatarAddress.Serialize(),
                [GroupIdKey] = GroupId.Serialize(),
                [SummonCountKey] = SummonCount.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
            GroupId = plainValue[GroupIdKey].ToInteger();
            SummonCount = plainValue[SummonCountKey].ToInteger();
        }

        public static IEnumerable<(int, Equipment)> SimulateSummon(
            string addressesHex,
            AgentState agentState,
            EquipmentItemRecipeSheet recipeSheet,
            EquipmentItemSheet equipmentItemSheet,
            EquipmentItemSubRecipeSheetV2 equipmentItemSubRecipeSheetV2,
            EquipmentItemOptionSheet optionSheet,
            SkillSheet skillSheet,
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random,
            long blockIndex
        )
        {
            summonCount = SummonHelper.CalculateSummonCount(summonCount);

            var result = new List<(int, Equipment)>();
            for (var i = 0; i < summonCount; i++)
            {
                var recipeId = SummonHelper.GetSummonRecipeIdByRandom(summonRow, random);

                // Validate RecipeId
                var recipeRow = recipeSheet.OrderedList.FirstOrDefault(r => r.Id == recipeId);
                if (recipeRow is null)
                {
                    throw new SheetRowNotFoundException(
                        addressesHex,
                        nameof(EquipmentItemRecipeSheet),
                        recipeId
                    );
                }

                // Validate Recipe ResultEquipmentId
                if (!equipmentItemSheet.TryGetValue(recipeRow.ResultEquipmentId,
                        out var equipmentRow))
                {
                    throw new SheetRowNotFoundException(
                        addressesHex,
                        nameof(equipmentItemSheet),
                        recipeRow.ResultEquipmentId);
                }

                // Validate subRecipeId
                if (recipeRow.SubRecipeIds.Count == 0)
                {
                    throw new InvalidRecipeIdException(
                        $"Recipe {recipeId} does not have any subRecipe.");
                }

                var subRecipeId = recipeRow.SubRecipeIds[0];
                if (!equipmentItemSubRecipeSheetV2.TryGetValue(subRecipeId, out var subRecipeRow))
                {
                    throw new SheetRowNotFoundException(
                        addressesHex,
                        nameof(EquipmentItemSubRecipeSheetV2),
                        subRecipeId
                    );
                }

                // Create Equipment
                var equipment = (Equipment)ItemFactory.CreateItemUsable(
                    equipmentRow,
                    random.GenerateRandomGuid(),
                    blockIndex
                );

                AddAndUnlockOption(
                    agentState,
                    equipment,
                    random,
                    subRecipeRow,
                    optionSheet,
                    skillSheet
                );
                result.Add((recipeId, equipment));
            }

            return result;
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug($"{addressesHex} AuraSummon Exec. Started.");

            var agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex} Aborted as the agent state of the signer was failed to load.");
            }

            if (!states.TryGetAvatarState(context.Signer, AvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex} Aborted as the avatar state of the signer was failed to load.");
            }

            if (!SummonHelper.CheckSummonCountIsValid(SummonCount))
            {
                throw new InvalidSummonCountException(
                    $"{addressesHex} Given summonCount {SummonCount} is not valid. Please use 1 or 10 or 100"
                );
            }

            // Validate Work
            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(SummonSheet),
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSheet),
                typeof(MaterialItemSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(EquipmentItemOptionSheet),
                typeof(SkillSheet),
            });

            var summonSheet = sheets.GetSheet<SummonSheet>();
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();

            var summonRow = summonSheet.OrderedList.FirstOrDefault(row => row.GroupId == GroupId);
            if (summonRow is null)
            {
                throw new RowNotInTableException(
                    $"{addressesHex} Failed to get {GroupId} in AuraSummonSheet");
            }

            // Use materials
            var inventory = avatarState.inventory;
            var material = materialSheet.OrderedList.First(m => m.Id == summonRow.CostMaterial);
            if (!inventory.RemoveFungibleItem(material.ItemId, context.BlockIndex,
                    summonRow.CostMaterialCount * SummonCount))
            {
                throw new NotEnoughMaterialException(
                    $"{addressesHex} Aborted as the player has no enough material ({summonRow.CostMaterial} * {summonRow.CostMaterialCount})");
            }

            // Transfer Cost NCG first for fast-fail
            if (summonRow.CostNcg > 0L)
            {
                var arenaSheet = states.GetSheet<ArenaSheet>();
                var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
                var feeStoreAddress =
                    ArenaHelper.DeriveArenaAddress(arenaData.ChampionshipId, arenaData.Round);

                states = states.TransferAsset(
                    context,
                    context.Signer,
                    feeStoreAddress,
                    states.GetGoldCurrency() * summonRow.CostNcg * SummonCount
                );
            }

            var random = context.GetRandom();
            var summonResult = SimulateSummon(
                addressesHex, agentState,
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                sheets.GetSheet<SkillSheet>(),
                summonRow, SummonCount,
                random, context.BlockIndex
            );

            foreach (var (recipeId, equipment) in summonResult)
            {
                // Add or update equipment
                avatarState.questList.UpdateCombinationEquipmentQuest(recipeId);
                avatarState.UpdateFromCombination(equipment);
                avatarState.UpdateQuestRewards(materialSheet);
            }

            Log.Debug(
                $"{addressesHex} AuraSummon Exec. finished: {DateTimeOffset.UtcNow - started} Elapsed");

            avatarState.blockIndex = context.BlockIndex;
            avatarState.updatedAt = context.BlockIndex;

            // Set states
            return states
                .SetAvatarState(AvatarAddress, avatarState)
                .SetAgentState(context.Signer, agentState);
        }

        public static void AddAndUnlockOption(
            AgentState agentState,
            Equipment equipment,
            IRandom random,
            EquipmentItemSubRecipeSheetV2.Row subRecipe,
            EquipmentItemOptionSheet optionSheet,
            SkillSheet skillSheet
        )
        {
            foreach (var optionInfo in subRecipe.Options
                         .OrderByDescending(e => e.Ratio)
                         .ThenBy(e => e.Id))
            {
                if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                {
                    continue;
                }

                var value = random.Next(1, GameConfig.MaximumProbability + 1);
                var ratio = optionInfo.Ratio;

                if (value > ratio)
                {
                    continue;
                }

                if (optionRow.StatType != StatType.NONE)
                {
                    var stat = CombinationEquipment5.GetStat(optionRow, random);
                    equipment.StatsMap.AddStatAdditionalValue(stat.StatType, stat.BaseValue);
                    equipment.optionCountFromCombination++;
                }
                else
                {
                    var skill = CombinationEquipment.GetSkill(optionRow, skillSheet, random);
                    if (skill is null) continue;

                    equipment.Skills.Add(skill);
                    equipment.optionCountFromCombination++;
                }
            }
        }
    }
}
