using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Battle;
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Event;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using Nekoyume.TableData.Rune;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Action for participating in an event dungeon battle.
    /// This action allows an avatar to battle in a specific stage of an event dungeon,
    /// consuming tickets and receiving rewards upon victory.
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2195
    /// </summary>
    [Serializable]
    [ActionType(ActionTypeText)]
    public class EventDungeonBattle : GameAction, IEventDungeonBattleV2
    {
        /// <summary>
        /// The action type identifier for event dungeon battle.
        /// </summary>
        private const string ActionTypeText = "event_dungeon_battle6";

        /// <summary>
        /// The number of battles to play in a single action execution.
        /// Currently fixed to 1 for this action type.
        /// </summary>
        public const int PlayCount = 1;

        /// <summary>
        /// The address of the avatar that will participate in the event dungeon battle.
        /// </summary>
        public Address AvatarAddress;

        /// <summary>
        /// The ID of the event schedule that defines when the event dungeon is available.
        /// </summary>
        public int EventScheduleId;

        /// <summary>
        /// The ID of the event dungeon to battle in.
        /// </summary>
        public int EventDungeonId;

        /// <summary>
        /// The ID of the specific stage within the event dungeon to battle.
        /// </summary>
        public int EventDungeonStageId;

        /// <summary>
        /// A list of equipment item GUIDs to be equipped for the battle.
        /// </summary>
        public List<Guid> Equipments;

        /// <summary>
        /// A list of costume item GUIDs to be equipped for the battle.
        /// </summary>
        public List<Guid> Costumes;

        /// <summary>
        /// A list of food item GUIDs to be consumed during the battle.
        /// </summary>
        public List<Guid> Foods;

        /// <summary>
        /// Whether to automatically purchase a ticket if the avatar doesn't have enough tickets.
        /// If true and tickets are insufficient, the action will attempt to buy a ticket using currency.
        /// </summary>
        public bool BuyTicketIfNeeded;

        /// <summary>
        /// A list of rune slot information specifying which runes to use in the battle.
        /// </summary>
        public List<RuneSlotInfo> RuneInfos;

        Address IEventDungeonBattleV2.AvatarAddress => AvatarAddress;
        int IEventDungeonBattleV2.EventScheduleId => EventScheduleId;
        int IEventDungeonBattleV2.EventDungeonId => EventDungeonId;
        int IEventDungeonBattleV2.EventDungeonStageId => EventDungeonStageId;
        IEnumerable<Guid> IEventDungeonBattleV2.Equipments => Equipments;
        IEnumerable<Guid> IEventDungeonBattleV2.Costumes => Costumes;
        IEnumerable<Guid> IEventDungeonBattleV2.Foods => Foods;
        IEnumerable<IValue> IEventDungeonBattleV2.RuneSlotInfos =>
            RuneInfos.Select(x => x.Serialize());
        bool IEventDungeonBattleV2.BuyTicketIfNeeded => BuyTicketIfNeeded;

        /// <summary>
        /// Serializes the action's data into a Bencodex dictionary.
        /// The data is stored as a list under the key "l" containing:
        /// AvatarAddress, EventScheduleId, EventDungeonId, EventDungeonStageId,
        /// Equipments, Costumes, Foods, BuyTicketIfNeeded, and RuneInfos.
        /// </summary>
        /// <returns>
        /// An immutable dictionary containing the serialized action data.
        /// </returns>
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
                    .Add(BuyTicketIfNeeded.Serialize())
                    .Add(RuneInfos.OrderBy(x => x.SlotIndex).Select(x => x.Serialize())
                        .Serialize());

                return new Dictionary<string, IValue>
                {
                    { "l", list },
                }.ToImmutableDictionary();
            }
        }

        /// <summary>
        /// Deserializes the action's data from a Bencodex dictionary.
        /// Expects a list under the key "l" containing at least 9 items in the order:
        /// AvatarAddress, EventScheduleId, EventDungeonId, EventDungeonStageId,
        /// Equipments, Costumes, Foods, BuyTicketIfNeeded, and RuneInfos.
        /// </summary>
        /// <param name="plainValue">
        /// The Bencodex dictionary containing the serialized action data.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when the plainValue doesn't contain the expected key "l",
        /// when "l" is not a Bencodex list, or when the list has fewer than 9 items.
        /// </exception>
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

            if (list.Count < 9)
            {
                throw new ArgumentException("'l' must contain at least 9 items");
            }

            AvatarAddress = list[0].ToAddress();
            EventScheduleId = list[1].ToInteger();
            EventDungeonId = list[2].ToInteger();
            EventDungeonStageId = list[3].ToInteger();
            Equipments = ((List)list[4]).ToList(StateExtensions.ToGuid);
            Costumes = ((List)list[5]).ToList(StateExtensions.ToGuid);
            Foods = ((List)list[6]).ToList(StateExtensions.ToGuid);
            BuyTicketIfNeeded = list[7].ToBoolean();
            RuneInfos = list[8].ToList(x => new RuneSlotInfo((List)x));
        }

        /// <summary>
        /// Executes the event dungeon battle action.
        /// This method performs the following operations:
        /// 1. Validates the avatar state and event dungeon information
        /// 2. Checks and consumes event dungeon tickets (or purchases if needed)
        /// 3. Validates equipment, costumes, foods, and runes
        /// 4. Updates rune and item slot states
        /// 5. Simulates the battle using StageSimulator
        /// 6. Applies battle results to the avatar state (experience, rewards, etc.)
        /// 7. Updates the event dungeon info if the stage is cleared
        /// </summary>
        /// <param name="context">
        /// The action context containing block index, signer, and random seed.
        /// </param>
        /// <returns>
        /// The updated world state after executing the battle.
        /// </returns>
        /// <exception cref="FailedLoadStateException">
        /// Thrown when the avatar state cannot be loaded.
        /// </exception>
        /// <exception cref="NotEnoughEventDungeonTicketsException">
        /// Thrown when the avatar doesn't have enough tickets and BuyTicketIfNeeded is false.
        /// </exception>
        /// <exception cref="StageNotClearedException">
        /// Thrown when attempting to battle a stage without clearing the previous stage.
        /// </exception>
        /// <exception cref="SheetRowNotFoundException">
        /// Thrown when required sheet data cannot be found.
        /// </exception>
        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Execute() start",
                ActionTypeText,
                addressesHex);

            var sw = new Stopwatch();
            // Get AvatarState
            sw.Start();
            if (!states.TryGetAvatarState(
                    context.Signer,
                    AvatarAddress,
                    out var avatarState))
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

            var collectionExist = states.TryGetCollectionState(AvatarAddress, out var collectionState);
            // Get sheets
            sw.Restart();
            var sheetTypes = new List<Type>
            {
                typeof(EventScheduleSheet),
                typeof(EventDungeonSheet),
                typeof(EventDungeonStageSheet),
                typeof(EventDungeonStageWaveSheet),
                typeof(EnemySkillSheet),
                typeof(CostumeStatSheet),
                typeof(MaterialItemSheet),
                typeof(RuneListSheet),
                typeof(RuneLevelBonusSheet),
                typeof(BuffLimitSheet),
                typeof(BuffLinkSheet),
            };
            if (collectionExist)
            {
                sheetTypes.Add(typeof(CollectionSheet));
            }
            var sheets = states.GetSheets(
                containSimulatorSheets: true,
                containValidateItemRequirementSheets: true,
                sheetTypes: sheetTypes
);
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

            var gameConfigState = states.GetGameConfigState();
            var equipmentList = avatarState.ValidateEquipmentsV3(
                Equipments, context.BlockIndex, gameConfigState);
            var costumeList = avatarState.ValidateCostumeV2(Costumes, gameConfigState);
            var foodIds = avatarState.ValidateConsumableV2(Foods, context.BlockIndex, gameConfigState);
            var equipmentAndCostumes = Equipments.Concat(Costumes);
            avatarState.EquipItems(equipmentAndCostumes);
            avatarState.ValidateItemRequirement(
                costumeList.Select(e => e.Id).Concat(foodIds).ToList(),
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
            var eventDungeonInfo = states.GetLegacyState(eventDungeonInfoAddr)
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

            if (!eventDungeonInfo.TryUseTickets(1))
            {
                if (!BuyTicketIfNeeded)
                {
                    throw new NotEnoughEventDungeonTicketsException(
                        ActionTypeText,
                        addressesHex,
                        1,
                        eventDungeonInfo.RemainingTickets);
                }

                var currency = states.GetGoldCurrency();
                var cost = scheduleRow.GetDungeonTicketCost(
                    eventDungeonInfo.NumberOfTicketPurchases,
                    currency);
                if (cost.Sign > 0)
                {
                    var feeAddress = Addresses.RewardPool;
                    // TODO: [GuildMigration] Remove this after migration
                    if (states.GetDelegationMigrationHeight() is long migrationHeight
                        && context.BlockIndex < migrationHeight)
                    {
                        feeAddress = Addresses.EventDungeon;
                    }

                    states = states.TransferAsset(context, context.Signer, feeAddress, cost);
                }

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
                "[{ActionTypeString}][{AddressesHex}] Validate event dungeon info: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);

            // update rune slot
            var runeSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var runeSlotState = states.TryGetLegacyState(runeSlotStateAddress, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Adventure);
            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            runeSlotState.UpdateSlot(RuneInfos, runeListSheet);
            states = states.SetLegacyState(runeSlotStateAddress, runeSlotState.Serialize());

            // update item slot
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var itemSlotState = states.TryGetLegacyState(itemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Adventure);
            itemSlotState.UpdateEquipment(Equipments);
            itemSlotState.UpdateCostumes(Costumes);
            states = states.SetLegacyState(itemSlotStateAddress, itemSlotState.Serialize());

            // Simulate
            sw.Restart();
            var exp = scheduleRow.GetStageExp(
                EventDungeonStageId.ToEventDungeonStageNumber(),
                PlayCount);
            var simulatorSheets = sheets.GetSimulatorSheets();
            var runeStates = states.GetRuneState(AvatarAddress, out var migrateRequired);
            if (migrateRequired)
            {
                states = states.SetRuneState(AvatarAddress, runeStates);
            }

            // just validate
            foreach (var runeSlotInfo in RuneInfos)
            {
                runeStates.GetRuneState(runeSlotInfo.RuneId);
            }

            var random = context.GetRandom();
            var collectionModifiers = new List<StatModifier>();
            if (collectionExist)
            {
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
                collectionModifiers = collectionState.GetModifiers(collectionSheet);
            }

            var buffLimitSheet = sheets.GetSheet<BuffLimitSheet>();
            var simulator = new StageSimulator(
                random,
                avatarState,
                Foods,
                runeStates,
                runeSlotState,
                new List<Skill>(),
                EventDungeonId,
                EventDungeonStageId,
                stageRow,
                sheets.GetSheet<EventDungeonStageWaveSheet>()[EventDungeonStageId],
                eventDungeonInfo.IsCleared(EventDungeonStageId),
                exp,
                simulatorSheets,
                sheets.GetSheet<EnemySkillSheet>(),
                sheets.GetSheet<CostumeStatSheet>(),
                StageSimulator.GetWaveRewards(
                    random,
                    stageRow,
                    sheets.GetSheet<MaterialItemSheet>(),
                    PlayCount),
                collectionModifiers,
                buffLimitSheet,
                sheets.GetSheet<BuffLinkSheet>(),
                shatterStrikeMaxDamage: gameConfigState.ShatterStrikeMaxDamage);
            simulator.Simulate();
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Simulate: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);


            var characterSheet = sheets.GetSheet<CharacterSheet>();
            var runeLevelBonusSheet = sheets.GetSheet<RuneLevelBonusSheet>();
            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            if (!characterSheet.TryGetValue(avatarState.characterId, out var myCharacterRow))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }
            var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(
                runeStates,
                runeListSheet,
                runeLevelBonusSheet
            );

            var runeOptions = RuneHelper.GetRuneOptions(RuneInfos, runeStates, runeOptionSheet);

            var cp = CPHelper.TotalCP(
                equipmentList,
                costumeList,
                runeOptions,
                avatarState.level,
                myCharacterRow,
                costumeStatSheet,
                collectionModifiers,
                runeLevelBonus
            );

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
            states = states
                .SetAvatarState(AvatarAddress, avatarState)
                .SetLegacyState(eventDungeonInfoAddr, eventDungeonInfo.Serialize())
                .SetCp(AvatarAddress, BattleType.Adventure, cp);

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
