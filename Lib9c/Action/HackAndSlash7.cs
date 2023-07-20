﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V200020AccidentObsoleteIndex)]
    [ActionType("hack_and_slash7")]
    public class HackAndSlash7 : GameAction, IHackAndSlashV2
    {
        public List<Guid> costumes;
        public List<Guid> equipments;
        public List<Guid> foods;
        public int worldId;
        public int stageId;
        public Address avatarAddress;
        public Address WeeklyArenaAddress;
        public Address RankingMapAddress;

        IEnumerable<Guid> IHackAndSlashV2.Costumes => costumes;
        IEnumerable<Guid> IHackAndSlashV2.Equipments => equipments;
        IEnumerable<Guid> IHackAndSlashV2.Foods => foods;
        int IHackAndSlashV2.WorldId => worldId;
        int IHackAndSlashV2.StageId => stageId;
        Address IHackAndSlashV2.AvatarAddress => avatarAddress;
        Address IHackAndSlashV2.WeeklyArenaAddress => WeeklyArenaAddress;
        Address IHackAndSlashV2.RankingMapAddress => RankingMapAddress;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["costumes"] = new List(costumes.OrderBy(i => i).Select(e => e.Serialize())),
                ["equipments"] = new List(equipments.OrderBy(i => i).Select(e => e.Serialize())),
                ["foods"] = new List(foods.OrderBy(i => i).Select(e => e.Serialize())),
                ["worldId"] = worldId.Serialize(),
                ["stageId"] = stageId.Serialize(),
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["weeklyArenaAddress"] = WeeklyArenaAddress.Serialize(),
                ["rankingMapAddress"] = RankingMapAddress.Serialize(),
            }.ToImmutableDictionary();


        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            costumes =  ((List) plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            equipments = ((List) plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            foods = ((List) plainValue["foods"]).Select(e => e.ToGuid()).ToList();
            worldId = plainValue["worldId"].ToInteger();
            stageId = plainValue["stageId"].ToInteger();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            WeeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
            RankingMapAddress = plainValue["rankingMapAddress"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            IActionContext ctx = context;
            var states = ctx.PreviousState;
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                states = states.SetState(RankingMapAddress, MarkChanged);
                states = states.SetState(avatarAddress, MarkChanged);
                states = states.SetState(WeeklyArenaAddress, MarkChanged);
                states = states
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}HAS exec started", addressesHex);

            if (!states.TryGetAvatarStateV2(ctx.Signer, avatarAddress, out AvatarState avatarState, out _))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (avatarState.RankingMapAddress != RankingMapAddress)
            {
                throw new InvalidAddressException($"{addressesHex}Invalid ranking map address");
            }

            // worldId와 stageId가 유효한지 확인합니다.
            var worldSheet = states.GetSheet<WorldSheet>();

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

            var stageSheet = states.GetSheet<StageSheet>();
            if (!stageSheet.TryGetValue(stageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(StageSheet), stageId);
            }

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

            avatarState.ValidateEquipmentsV2(equipments, context.BlockIndex);
            avatarState.ValidateConsumable(foods, context.BlockIndex);
            avatarState.ValidateCostume(costumes);

            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();

            sw.Restart();
            if (avatarState.actionPoint < stageRow.CostAP)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: " +
                    $"{avatarState.actionPoint} < {stageRow.CostAP}"
                );
            }

            avatarState.actionPoint -= stageRow.CostAP;

            var items = equipments.Concat(costumes);
            avatarState.EquipItems(items);
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Unequip items: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            // Update QuestList only when QuestSheet.Count is greater than QuestList.Count
            var questList = avatarState.questList;
            var questSheet = states.GetQuestSheet();
            if (questList.Count() < questSheet.Count)
            {
                questList.UpdateListV1(
                    2,
                    questSheet,
                    states.GetSheet<QuestRewardSheet>(),
                    states.GetSheet<QuestItemRewardSheet>(),
                    states.GetSheet<EquipmentItemRecipeSheet>());
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Update QuestList: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var characterSheet = states.GetSheet<CharacterSheet>();
            var simulator = new StageSimulatorV1(
                ctx.Random,
                avatarState,
                foods,
                worldId,
                stageId,
                states.GetStageSimulatorSheetsV1(),
                costumeStatSheet,
                StageSimulatorV1.ConstructorVersionV100025);

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Initialize Simulator: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            simulator.SimulateV3();
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Simulator.SimulateV2(): {Elapsed}", addressesHex, sw.Elapsed);

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

            sw.Restart();
            if (simulator.Log.IsClear)
            {
                var worldUnlockSheet = states.GetSheet<WorldUnlockSheet>();
                simulator.Player.worldInformation.ClearStage(
                    worldId,
                    stageId,
                    ctx.BlockIndex,
                    worldSheet,
                    worldUnlockSheet
                );
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS ClearStage: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            avatarState.Update(simulator);

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            avatarState.UpdateQuestRewards2(materialSheet);

            avatarState.updatedAt = ctx.BlockIndex;
            avatarState.mailBox.CleanUp();
            states = states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            if (states.TryGetState(RankingMapAddress, out Dictionary d) && simulator.Log.IsClear)
            {
                var ranking = new RankingMapState(d);
                ranking.Update(avatarState);

                sw.Stop();
                Log.Verbose("{AddressesHex}HAS Update RankingState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                var serialized = ranking.Serialize();

                sw.Stop();
                Log.Verbose("{AddressesHex}HAS Serialize RankingState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();
                states = states.SetState(RankingMapAddress, serialized);
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Set RankingState: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            if (simulator.Log.stageId >= GameConfig.RequireClearedStageLevel.ActionsInRankingBoard &&
                simulator.Log.IsClear &&
                states.TryGetState(WeeklyArenaAddress, out Dictionary weeklyDict))
            {
                var weekly = new WeeklyArenaState(weeklyDict);
                if (!weekly.Ended)
                {
                    if (weekly.ContainsKey(avatarAddress))
                    {
                        var info = weekly[avatarAddress];
                        info.UpdateV2(avatarState, characterSheet, costumeStatSheet);
                        weekly.Update(info);
                    }
                    else
                    {
                        weekly.SetV2(avatarState, characterSheet, costumeStatSheet);
                    }

                    sw.Stop();
                    Log.Verbose("{AddressesHex}HAS Update WeeklyArenaState: {Elapsed}", addressesHex, sw.Elapsed);

                    sw.Restart();
                    var weeklySerialized = weekly.Serialize();
                    sw.Stop();
                    Log.Verbose("{AddressesHex}HAS Serialize RankingState: {Elapsed}", addressesHex, sw.Elapsed);

                    states = states.SetState(weekly.address, weeklySerialized);
                }
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}HAS Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states;
        }
    }
}
