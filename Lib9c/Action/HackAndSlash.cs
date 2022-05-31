using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/967
    /// Updated at https://github.com/planetarium/lib9c/pull/992
    /// </summary>
    [Serializable]
    [ActionType("hack_and_slash14")]
    public class HackAndSlash : GameAction
    {
        public List<Guid> costumes;
        public List<Guid> equipments;
        public List<Guid> foods;
        public int worldId;
        public int stageId;
        public int? stageBuffId;
        public Address avatarAddress;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["costumes"] = new List(costumes.OrderBy(i => i).Select(e => e.Serialize())),
                ["equipments"] = new List(equipments.OrderBy(i => i).Select(e => e.Serialize())),
                ["foods"] = new List(foods.OrderBy(i => i).Select(e => e.Serialize())),
                ["worldId"] = worldId.Serialize(),
                ["stageId"] = stageId.Serialize(),
                ["stageBuffId"] = stageBuffId.Serialize(),
                ["avatarAddress"] = avatarAddress.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            foods = ((List)plainValue["foods"]).Select(e => e.ToGuid()).ToList();
            worldId = plainValue["worldId"].ToInteger();
            stageId = plainValue["stageId"].ToInteger();
            stageBuffId = plainValue["stageBuffId"].ToNullableInteger();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                states = states.SetState(avatarAddress, MarkChanged);
                states = states
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}HAS exec started", addressesHex);

            if (worldId > 1)
            {
                if (worldId == GameConfig.MimisbrunnrWorldId)
                {
                    throw new InvalidWorldException($"{addressesHex}{worldId} can't execute HackAndSlash action.");
                }

                var unlockedWorldIdsAddress = avatarAddress.Derive("world_ids");

                // Unlock First.
                if (!states.TryGetState(unlockedWorldIdsAddress, out List rawIds))
                {
                    throw new InvalidWorldException();
                }

                List<int> unlockedWorldIds = rawIds.ToList(StateExtensions.ToInteger);
                if (!unlockedWorldIds.Contains(worldId))
                {
                    throw new InvalidWorldException();
                }
            }

            var sw = new Stopwatch();
            sw.Start();
            if (!states.TryGetAvatarStateV2(ctx.Signer, avatarAddress, out AvatarState avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Get AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var sheets = states.GetSheets(
                containQuestSheet: true,
                containStageSimulatorSheets: true,
                sheetTypes: new[]
                {
                    typeof(WorldSheet),
                    typeof(StageSheet),
                    typeof(SkillSheet),
                    typeof(QuestRewardSheet),
                    typeof(QuestItemRewardSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(CostumeStatSheet),
                    typeof(WorldUnlockSheet),
                    typeof(MaterialItemSheet),
                    typeof(ItemRequirementSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                    typeof(CrystalStageBuffGachaSheet),
                    typeof(CrystalRandomBuffSheet),
                });
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Get Sheets: {Elapsed}", addressesHex, sw.Elapsed);

            var worldSheet = sheets.GetSheet<WorldSheet>();
            if (!worldSheet.TryGetValue(worldId, out var worldRow, false))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(WorldSheet), worldId);
            }

            if (stageId < worldRow.StageBegin ||
                stageId > worldRow.StageEnd)
            {
                throw new SheetRowColumnException(
                    $"{addressesHex}{worldId} world is not contains {worldRow.Id} stage: " +
                    $"{worldRow.StageBegin}-{worldRow.StageEnd}");
            }

            sw.Restart();
            if (!sheets.GetSheet<StageSheet>().TryGetValue(stageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(StageSheet), stageId);
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Get StageSheet: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var worldInformation = avatarState.worldInformation;
            if (!worldInformation.TryGetWorld(worldId, out var world))
            {
                // NOTE: Add new World from WorldSheet
                worldInformation.AddAndUnlockNewWorld(worldRow, ctx.BlockIndex, worldSheet);
            }

            if (!world.IsUnlocked)
            {
                throw new InvalidWorldException($"{addressesHex}{worldId} is locked.");
            }

            if (world.StageBegin != worldRow.StageBegin ||
                world.StageEnd != worldRow.StageEnd)
            {
                worldInformation.UpdateWorld(worldRow);
            }

            if (world.IsStageCleared && stageId > world.StageClearedId + 1 ||
                !world.IsStageCleared && stageId != world.StageBegin)
            {
                throw new InvalidStageException(
                    $"{addressesHex}Aborted as the stage ({worldId}/{stageId}) is not cleared; " +
                    $"cleared stage: {world.StageClearedId}"
                );
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Validate World: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var equipmentList = avatarState.ValidateEquipmentsV2(equipments, context.BlockIndex);
            var foodIds = avatarState.ValidateConsumable(foods, context.BlockIndex);
            var costumeIds = avatarState.ValidateCostume(costumes);
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Validate Items: {Elapsed}", addressesHex, sw.Elapsed);

            if (avatarState.actionPoint < stageRow.CostAP)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: " +
                    $"{avatarState.actionPoint} < cost({stageRow.CostAP}))"
                );
            }

            var items = equipments.Concat(costumes);
            avatarState.EquipItems(items);
            avatarState.ValidateItemRequirement(
                costumeIds.Concat(foodIds).ToList(),
                equipmentList,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                addressesHex);

            avatarState.actionPoint -= stageRow.CostAP;
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Unequip items: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var questSheet = sheets.GetQuestSheet();
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS GetQuestSheet: {Elapsed}", addressesHex, sw.Elapsed);

            // Update QuestList only when QuestSheet.Count is greater than QuestList.Count
            var questList = avatarState.questList;
            if (questList.Count() < questSheet.Count)
            {
                sw.Restart();
                questList.UpdateList(
                    questSheet,
                    sheets.GetSheet<QuestRewardSheet>(),
                    sheets.GetSheet<QuestItemRewardSheet>(),
                    sheets.GetSheet<EquipmentItemRecipeSheet>());

                sw.Stop();
                Log.Verbose("{AddressesHex}HAS Update QuestList: {Elapsed}", addressesHex, sw.Elapsed);
            }

            sw.Restart();

            var buffStateAddress = Addresses.GetBuffStateAddressFromAvatarAddress(avatarAddress);
            HackAndSlashBuffState buffState;
            var buffSkillsOnWaveStart = new List<Model.Skill.BuffSkill>();
            var crystalRandomBuffSheet = sheets.GetSheet<CrystalRandomBuffSheet>();
            var skillSheet = sheets.GetSheet<SkillSheet>();
            if (stageBuffId.HasValue &&
                worldInformation.IsStageCleared(stageId))
            {
                if (states.TryGetState<List>(buffStateAddress, out var serialized))
                {
                    var newBuffState = new HackAndSlashBuffState(buffStateAddress, serialized);
                    buffState = newBuffState.StageId == stageId
                        ? newBuffState
                        : new HackAndSlashBuffState(buffStateAddress, stageId);
                }
                else
                {
                    buffState = new HackAndSlashBuffState(buffStateAddress, stageId);
                }

                if (buffState.BuffIds.Any())
                {
                    if (!buffState.BuffIds.Contains(stageBuffId.Value))
                    {
                        stageBuffId = buffState.BuffIds
                            .OrderBy(id => crystalRandomBuffSheet[id].Rank)
                            .ThenBy(id => id)
                            .First();
                    }

                    if (!crystalRandomBuffSheet.TryGetValue(stageBuffId.Value, out var row))
                    {
                        throw new SheetRowNotFoundException(addressesHex, nameof(CrystalRandomBuffSheet), stageBuffId.Value);
                    }

                    if (!skillSheet.TryGetValue(row.SkillId, out var skillRow))
                    {
                        throw new SheetRowNotFoundException(addressesHex, nameof(SkillSheet), row.SkillId);
                    }

                    var skill = new Model.Skill.BuffSkill(skillRow, 0, 100);
                    buffSkillsOnWaveStart.Add(skill);
                }
            }
            else
            {
                buffState = null;
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Get BuffState : {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();

            var simulator = new StageSimulator(
                ctx.Random,
                avatarState,
                foods,
                buffSkillsOnWaveStart,
                worldId,
                stageId,
                sheets.GetStageSimulatorSheets(),
                sheets.GetSheet<CostumeStatSheet>(),
                StageSimulator.ConstructorVersionV100080);

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Initialize Simulator: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            simulator.Simulate(1);
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Simulator.Simulate(): {Elapsed}", addressesHex, sw.Elapsed);

            Log.Verbose(
                "{AddressesHex}Execute HackAndSlash({AvatarAddress}); worldId: {WorldId}, stageId: {StageId}, result: {Result}, " +
                "clearWave: {ClearWave}, totalWave: {TotalWave}",
                addressesHex,
                avatarAddress,
                worldId,
                stageId,
                simulator.Log.result,
                simulator.Log.clearedWaveNumber,
                simulator.Log.waveCount
            );

            if (simulator.Log.IsClear)
            {
                sw.Restart();
                simulator.Player.worldInformation.ClearStage(
                    worldId,
                    stageId,
                    ctx.BlockIndex,
                    worldSheet,
                    sheets.GetSheet<WorldUnlockSheet>()
                );
                sw.Stop();
                Log.Verbose("{AddressesHex}HAS ClearStage: {Elapsed}", addressesHex, sw.Elapsed);
            }
            else
            {
                if (buffState != null)
                {
                    buffState.Update(simulator.Log.clearedWaveNumber,
                        sheets.GetSheet<CrystalStageBuffGachaSheet>());
                    states = states.SetState(buffStateAddress, buffState.Serialize());
                }
            }

            sw.Restart();
            avatarState.Update(simulator);
            avatarState.UpdateQuestRewards(sheets.GetSheet<MaterialItemSheet>());
            avatarState.updatedAt = ctx.BlockIndex;
            avatarState.mailBox.CleanUp();
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Update AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            states = states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Set States: {Elapsed}", addressesHex, sw.Elapsed);

            var totalElapsed = DateTimeOffset.UtcNow - started;
            Log.Verbose("{AddressesHex}HAS Total Executed Time: {Elapsed}", addressesHex, totalElapsed);
            return states;
        }
    }
}
