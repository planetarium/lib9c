﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Model.Event;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/1218
    /// </summary>
    [Serializable]
    [ActionType(ActionTypeText)]
    public class EventDungeonBattleV1 : GameAction
    {
        private const string ActionTypeText = "event_dungeon_battle";
        public const int PlayCount = 1;

        public Address AvatarAddress;
        public int EventScheduleId;
        public int EventDungeonId;
        public int EventDungeonStageId;
        public List<Guid> Equipments;
        public List<Guid> Costumes;
        public List<Guid> Foods;
        public bool BuyTicketIfNeeded;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var list = Bencodex.Types.List.Empty
                    .Add(AvatarAddress.Serialize())
                    .Add(EventScheduleId.Serialize())
                    .Add(EventDungeonId.Serialize())
                    .Add(EventDungeonStageId.Serialize())
                    .Add(new Bencodex.Types.List(
                        Equipments
                            .OrderBy(e => e)
                            .Select(e => e.Serialize())))
                    .Add(new Bencodex.Types.List(
                        Costumes
                            .OrderBy(e => e)
                            .Select(e => e.Serialize())))
                    .Add(new Bencodex.Types.List(
                        Foods
                            .OrderBy(e => e)
                            .Select(e => e.Serialize())))
                    .Add(BuyTicketIfNeeded.Serialize());

                return new Dictionary<string, IValue>
                {
                    { "l", list },
                }.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            if (!plainValue.TryGetValue("l", out var serialized))
            {
                throw new ArgumentException("plainValue must contain 'l'");
            }

            if (!(serialized is Bencodex.Types.List list))
            {
                throw new ArgumentException("'l' must be a bencodex list");
            }

            if (list.Count < 8)
            {
                throw new ArgumentException("'l' must contain at least 8 items");
            }

            AvatarAddress = list[0].ToAddress();
            EventScheduleId = list[1].ToInteger();
            EventDungeonId = list[2].ToInteger();
            EventDungeonStageId = list[3].ToInteger();
            Equipments = ((List)list[4]).ToList(StateExtensions.ToGuid);
            Costumes = ((List)list[5]).ToList(StateExtensions.ToGuid);
            Foods = ((List)list[6]).ToList(StateExtensions.ToGuid);
            BuyTicketIfNeeded = list[7].ToBoolean();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Execute() start",
                ActionTypeText,
                addressesHex);

            var sw = new Stopwatch();
            // Get AvatarState
            sw.Start();
            if (!states.TryGetAvatarStateV2(
                    context.Signer,
                    AvatarAddress,
                    out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(AvatarState),
                    AvatarAddress);
            }

            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] TryGetAvatarStateV2: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Get AvatarState

            // Get sheets
            sw.Restart();
            var sheets = states.GetSheets(
                containSimulatorSheets: true,
                containValidateItemRequirementSheets: true,
                sheetTypes: new[]
                {
                    typeof(EventScheduleSheet),
                    typeof(EventDungeonSheet),
                    typeof(EventDungeonStageSheet),
                    typeof(EventDungeonStageWaveSheet),
                    typeof(EnemySkillSheet),
                    typeof(CostumeStatSheet),
                    typeof(MaterialItemSheet),
                });
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Get sheets: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Get sheets

            // Validate fields.
            sw.Restart();
            var scheduleSheet = sheets.GetSheet<EventScheduleSheet>();
            var scheduleRow = scheduleSheet.ValidateFromActionForDungeon(
                context.BlockIndex,
                EventScheduleId,
                EventDungeonId,
                ActionTypeText,
                addressesHex);

            var dungeonSheet = sheets.GetSheet<EventDungeonSheet>();
            var dungeonRow = dungeonSheet.ValidateFromAction(
                EventDungeonId,
                EventDungeonStageId,
                ActionTypeText,
                addressesHex);

            var stageSheet = sheets.GetSheet<EventDungeonStageSheet>();
            var stageRow = stageSheet.ValidateFromAction(
                EventDungeonStageId,
                ActionTypeText,
                addressesHex);

            var equipmentList = avatarState.ValidateEquipmentsV2(Equipments, context.BlockIndex);
            var costumeIds = avatarState.ValidateCostume(Costumes);
            var foodIds = avatarState.ValidateConsumable(Foods, context.BlockIndex);
            var equipmentAndCostumes = Equipments.Concat(Costumes);
            avatarState.EquipItems(equipmentAndCostumes);
            avatarState.ValidateItemRequirement(
                costumeIds.Concat(foodIds).ToList(),
                equipmentList,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                addressesHex);

            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Validate fields: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Validate fields.

            // Validate avatar's event dungeon info.
            sw.Restart();
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(
                AvatarAddress,
                EventDungeonId);
            var eventDungeonInfo = states.GetState(eventDungeonInfoAddr)
                is Bencodex.Types.List serializedEventDungeonInfoList
                ? new EventDungeonInfo(serializedEventDungeonInfoList)
                : new EventDungeonInfo(remainingTickets: scheduleRow.DungeonTicketsMax);

            // Update tickets.
            {
                var blockRange = context.BlockIndex - scheduleRow.StartBlockIndex;
                if (blockRange > 0)
                {
                    var interval =
                        (int)(blockRange / scheduleRow.DungeonTicketsResetIntervalBlockRange);
                    if (interval > eventDungeonInfo.ResetTicketsInterval)
                    {
                        eventDungeonInfo.ResetTickets(
                            interval,
                            scheduleRow.DungeonTicketsMax);
                    }
                }
            }
            // ~Update tickets.

            if (!eventDungeonInfo.TryUseTickets(PlayCount))
            {
                if (!BuyTicketIfNeeded)
                {
                    throw new NotEnoughEventDungeonTicketsException(
                        ActionTypeText,
                        addressesHex,
                        PlayCount,
                        eventDungeonInfo.RemainingTickets);
                }

                var currency = states.GetGoldCurrency();
                var cost = scheduleRow.GetDungeonTicketCostV1(
                    eventDungeonInfo.NumberOfTicketPurchases);
                if (cost > 0L)
                {
                    states = states.TransferAsset(
                        context.Signer,
                        Addresses.EventDungeon,
                        cost * currency);
                }

                // NOTE: The number of ticket purchases should be increased
                //       even if [`cost`] is 0.
                eventDungeonInfo.IncreaseNumberOfTicketPurchases();
            }

            if (EventDungeonStageId != dungeonRow.StageBegin &&
                !eventDungeonInfo.IsCleared(EventDungeonStageId - 1))
            {
                throw new StageNotClearedException(
                    ActionTypeText,
                    addressesHex,
                    EventDungeonStageId - 1,
                    eventDungeonInfo.ClearedStageId);
            }

            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Validate fields: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Validate avatar's event dungeon info.

            // Simulate
            sw.Restart();
            var exp = scheduleRow.GetStageExp(
                EventDungeonStageId.ToEventDungeonStageNumber(),
                PlayCount);
            var simulator = new StageSimulator(
                context.Random,
                avatarState,
                Foods,
                new List<Skill>(),
                EventDungeonId,
                EventDungeonStageId,
                stageRow,
                sheets.GetSheet<EventDungeonStageWaveSheet>()[EventDungeonStageId],
                eventDungeonInfo.IsCleared(EventDungeonStageId),
                exp,
                sheets.GetSimulatorSheets(),
                sheets.GetSheet<EnemySkillSheet>(),
                sheets.GetSheet<CostumeStatSheet>(),
                StageSimulator.GetWaveRewards(
                    context.Random,
                    stageRow,
                    sheets.GetSheet<MaterialItemSheet>(),
                    PlayCount));
            simulator.Simulate();
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Simulate: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Simulate

            // Update avatar's event dungeon info.
            if (simulator.Log.IsClear)
            {
                sw.Restart();
                eventDungeonInfo.ClearStage(EventDungeonStageId);
                sw.Stop();
                Log.Verbose(
                    "[{ActionTypeString}][{AddressesHex}] Update event dungeon info: {Elapsed}",
                    ActionTypeText,
                    addressesHex,
                    sw.Elapsed);
            }
            // ~Update avatar's event dungeon info.

            // Apply player to avatar state
            sw.Restart();
            avatarState.Apply(simulator.Player, context.BlockIndex);
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Apply player to avatar state: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Apply player to avatar state

            // Set states
            sw.Restart();
            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(
                        AvatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        AvatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        AvatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize())
                    .SetState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());
            }
            else
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(
                        AvatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());
            }

            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Set states: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Set states

            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Total elapsed: {Elapsed}",
                ActionTypeText,
                addressesHex,
                DateTimeOffset.UtcNow - started);
            return states;
        }
    }
}
