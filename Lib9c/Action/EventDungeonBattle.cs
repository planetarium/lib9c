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
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Event;
using Nekoyume.Model.Skill;
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
    /// Represents an event dungeon battle action where players engage in combat within event dungeons.
    /// This action allows players to battle multiple times in a single transaction using TotalPlayCount.
    ///
    /// Key Features:
    /// - Multiple battle execution in one action (TotalPlayCount)
    /// - Event dungeon ticket management
    /// - Automatic ticket purchase when needed
    /// - Rune and equipment slot management
    /// - Collection-based stat modifiers
    /// - CP (Combat Power) calculation and updates
    ///
    /// This action modifies the following states:
    /// - AvatarState: Updates player's stats, inventory, and experience
    /// - EventDungeonInfo: Updates dungeon progress and ticket usage
    /// - RuneSlotState: Updates equipped runes for adventure battles
    /// - ItemSlotState: Updates equipped items for adventure battles
    /// - CP: Updates character's combat power
    ///
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2195
    /// </summary>
    [Serializable]
    [ActionType(ActionTypeText)]
    public class EventDungeonBattle : GameAction, IEventDungeonBattleV2
    {
        /// <summary>
        /// The action type identifier for this action.
        /// </summary>
        private const string ActionTypeText = "event_dungeon_battle6";

        /// <summary>
        /// Default play count for a single battle execution.
        /// </summary>
        public const int PlayCount = 1;

        /// <summary>
        /// Maximum number of tickets that can be used in a single action.
        /// </summary>
        public const int MaxTicketCount = 5;

        /// <summary>
        /// The address of the avatar participating in the battle.
        /// </summary>
        public Address AvatarAddress;

        /// <summary>
        /// The ID of the event schedule that determines the event period and rules.
        /// </summary>
        public int EventScheduleId;

        /// <summary>
        /// The ID of the specific event dungeon where the battle takes place.
        /// </summary>
        public int EventDungeonId;

        /// <summary>
        /// The ID of the specific stage within the event dungeon.
        /// </summary>
        public int EventDungeonStageId;

        /// <summary>
        /// List of equipment item IDs to be equipped during the battle.
        /// </summary>
        public List<Guid> Equipments;

        /// <summary>
        /// List of costume item IDs to be equipped during the battle.
        /// </summary>
        public List<Guid> Costumes;

        /// <summary>
        /// List of food item IDs to be consumed during the battle.
        /// </summary>
        public List<Guid> Foods;

        /// <summary>
        /// Whether to automatically purchase additional tickets if the current tickets are insufficient.
        /// </summary>
        public bool BuyTicketIfNeeded;

        /// <summary>
        /// List of rune slot information specifying which runes to equip.
        /// </summary>
        public List<RuneSlotInfo> RuneInfos;

        /// <summary>
        /// The total number of battles to execute in this action.
        /// Must be between 1 and MaxTicketCount (5).
        /// </summary>
        public int TotalPlayCount = 1;

        // Interface implementation for IEventDungeonBattleV2
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
        /// Serializes the action data into a Bencodex format for blockchain storage.
        /// The serialization order is important for backward compatibility.
        /// </summary>
        /// <returns>A dictionary containing the serialized action data.</returns>
        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                // Create a list with all action fields in the correct order
                var list = Bencodex.Types.List.Empty
                    .Add(AvatarAddress.Serialize())                    // [0] AvatarAddress
                    .Add(EventScheduleId.Serialize())                 // [1] EventScheduleId
                    .Add(EventDungeonId.Serialize())                  // [2] EventDungeonId
                    .Add(EventDungeonStageId.Serialize())             // [3] EventDungeonStageId
                    .Add(new Bencodex.Types.List(                     // [4] Equipments (sorted for consistency)
                        Equipments
                            .OrderBy(e => e)
                            .Select(e => e.Serialize())))
                    .Add(new Bencodex.Types.List(                     // [5] Costumes (sorted for consistency)
                        Costumes
                            .OrderBy(e => e)
                            .Select(e => e.Serialize())))
                    .Add(new Bencodex.Types.List(                     // [6] Foods (sorted for consistency)
                        Foods
                            .OrderBy(e => e)
                            .Select(e => e.Serialize())))
                    .Add(BuyTicketIfNeeded.Serialize())               // [7] BuyTicketIfNeeded
                    .Add(RuneInfos.OrderBy(x => x.SlotIndex)          // [8] RuneInfos (sorted by slot index)
                        .Select(x => x.Serialize())
                        .Serialize())
                    .Add(TotalPlayCount);                             // [9] TotalPlayCount (new field)

                return new Dictionary<string, IValue>
                {
                    { "l", list },
                }.ToImmutableDictionary();
            }
        }

        /// <summary>
        /// Deserializes the action data from Bencodex format.
        /// Handles both legacy format (9 fields) and new format (10 fields with TotalPlayCount).
        /// </summary>
        /// <param name="plainValue">The serialized action data to deserialize.</param>
        /// <exception cref="ArgumentException">Thrown when the plainValue format is invalid.</exception>
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

            // Deserialize all required fields
            AvatarAddress = list[0].ToAddress();
            EventScheduleId = list[1].ToInteger();
            EventDungeonId = list[2].ToInteger();
            EventDungeonStageId = list[3].ToInteger();
            Equipments = ((List)list[4]).ToList(StateExtensions.ToGuid);
            Costumes = ((List)list[5]).ToList(StateExtensions.ToGuid);
            Foods = ((List)list[6]).ToList(StateExtensions.ToGuid);
            BuyTicketIfNeeded = list[7].ToBoolean();
            RuneInfos = list[8].ToList(x => new RuneSlotInfo((List)x));

            // Handle TotalPlayCount field (backward compatibility)
            if (list.Count > 9)
            {
                TotalPlayCount = (Integer)list[9];
            }
            else
            {
                TotalPlayCount = 1; // Default value for legacy format
            }
        }

        /// <summary>
        /// Executes the event dungeon battle action.
        /// This method performs multiple battles based on TotalPlayCount, updating the avatar's
        /// state, experience, and dungeon progress accordingly.
        /// </summary>
        /// <param name="context">The action execution context containing block information and signer.</param>
        /// <returns>The updated world state after executing the action.</returns>
        /// <exception cref="PlayCountIsZeroException">Thrown when TotalPlayCount is less than 1.</exception>
        /// <exception cref="ExceedPlayCountException">Thrown when TotalPlayCount exceeds MaxTicketCount.</exception>
        /// <exception cref="FailedLoadStateException">Thrown when the avatar state cannot be loaded.</exception>
        /// <exception cref="NotEnoughEventDungeonTicketsException">Thrown when there are insufficient tickets and BuyTicketIfNeeded is false.</exception>
        /// <exception cref="StageNotClearedException">Thrown when attempting to access a stage that hasn't been cleared yet.</exception>
        public override IWorld Execute(IActionContext context)
        {
            // Use gas for action execution
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;

            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Execute() start",
                ActionTypeText,
                addressesHex);

            var sw = new Stopwatch();

            // Step 1: Validate TotalPlayCount
            sw.Start();
            if (TotalPlayCount < 1)
            {
                throw new PlayCountIsZeroException(
                    $"{addressesHex}playCount must not be zero or negative. " +
                    $"Total play count : {TotalPlayCount}");
            }

            if (TotalPlayCount > MaxTicketCount)
            {
                throw new ExceedPlayCountException(
                    "Exceeded the amount of tickets that can be used " +
                    $"playCount : {TotalPlayCount} > maxTicketCount : {MaxTicketCount}");
            }

            // Step 2: Load and validate avatar state
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

            // Step 3: Check collection state and prepare sheet types
            var collectionExist = states.TryGetCollectionState(AvatarAddress, out var collectionState);
            sw.Restart();

            // Define all required sheet types for the action
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

            // Add collection sheet if collection state exists
            if (collectionExist)
            {
                sheetTypes.Add(typeof(CollectionSheet));
            }

            // Load all required sheets
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

            // Step 4: Validate all input fields and requirements
            sw.Restart();

            // Validate event schedule, dungeon, and stage
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

            // Validate equipment, costumes, and food items
            var gameConfigState = states.GetGameConfigState();
            var equipmentList = avatarState.ValidateEquipmentsV3(
                Equipments, context.BlockIndex, gameConfigState);
            var costumeList = avatarState.ValidateCostumeV2(Costumes, gameConfigState);
            var foodIds = avatarState.ValidateConsumableV2(Foods, context.BlockIndex, gameConfigState);

            // Equip items and validate requirements
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

            // Step 5: Validate and update event dungeon info
            sw.Restart();

            // Get or create event dungeon info
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(
                AvatarAddress,
                EventDungeonId);
            var eventDungeonInfo = states.GetLegacyState(eventDungeonInfoAddr)
                is Bencodex.Types.List serializedEventDungeonInfoList
                ? new EventDungeonInfo(serializedEventDungeonInfoList)
                : new EventDungeonInfo(remainingTickets: scheduleRow.DungeonTicketsMax);

            // Update tickets based on block interval
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

            // Step 6: Handle ticket usage and purchase if needed
            if (!eventDungeonInfo.TryUseTickets(TotalPlayCount))
            {
                if (!BuyTicketIfNeeded)
                {
                    throw new NotEnoughEventDungeonTicketsException(
                        ActionTypeText,
                        addressesHex,
                        PlayCount,
                        eventDungeonInfo.RemainingTickets);
                }

                // Calculate and purchase additional tickets
                var currency = states.GetGoldCurrency();
                var totalCost = 0 * currency;
                var requiredPurchaseCount = TotalPlayCount - eventDungeonInfo.RemainingTickets;

                for (int i = 0; i < requiredPurchaseCount; i++)
                {
                    var cost = scheduleRow.GetDungeonTicketCost(
                        eventDungeonInfo.NumberOfTicketPurchases,
                        currency);
                    totalCost += cost;

                    // NOTE: The number of ticket purchases should be increased
                    //       even if [`cost`] is 0.
                    eventDungeonInfo.IncreaseNumberOfTicketPurchases();
                }

                // Transfer payment if cost is greater than 0
                if (totalCost.Sign > 0)
                {
                    var feeAddress = Addresses.RewardPool;

                    // TODO: [GuildMigration] Remove this after migration
                    if (states.GetDelegationMigrationHeight() is long migrationHeight
                        && context.BlockIndex < migrationHeight)
                    {
                        feeAddress = Addresses.EventDungeon;
                    }

                    states = states.TransferAsset(
                        context,
                        context.Signer,
                        feeAddress,
                        totalCost
                    );
                }
            }

            // Step 7: Validate stage progression
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

            // Step 8: Update rune slot state
            var runeSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var runeSlotState = states.TryGetLegacyState(runeSlotStateAddress, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Adventure);
            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            runeSlotState.UpdateSlot(RuneInfos, runeListSheet);
            states = states.SetLegacyState(runeSlotStateAddress, runeSlotState.Serialize());

            // Step 9: Update item slot state
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var itemSlotState = states.TryGetLegacyState(itemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Adventure);
            itemSlotState.UpdateEquipment(Equipments);
            itemSlotState.UpdateCostumes(Costumes);
            states = states.SetLegacyState(itemSlotStateAddress, itemSlotState.Serialize());

            // Step 10: Execute multiple battles based on TotalPlayCount
            sw.Restart();
            var simulatorSheets = sheets.GetSimulatorSheets();
            var runeStates = states.GetRuneState(AvatarAddress, out var migrateRequired);
            if (migrateRequired)
            {
                states = states.SetRuneState(AvatarAddress, runeStates);
            }

            // Validate rune states
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

            // Execute battles for each play count
            for (int i = 0; i < TotalPlayCount; i++)
            {
                // Get rewards for this battle
                var rewards = StageSimulator.GetWaveRewards(
                    random,
                    stageRow,
                    sheets.GetSheet<MaterialItemSheet>());

                sw.Restart();

                // Calculate experience for this battle
                var exp = scheduleRow.GetStageExp(
                    EventDungeonStageId.ToEventDungeonStageNumber(),
                    1); // Each battle gets base experience

                // Create and run battle simulator
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
                    rewards,
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

                // Step 11: Update event dungeon progress if stage is cleared
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

                // Step 12: Apply battle results to avatar state
                sw.Restart();
                avatarState.Apply(simulator.Player, context.BlockIndex);
                sw.Stop();

                Log.Verbose(
                    "[{ActionTypeString}][{AddressesHex}] Apply player to avatar state: {Elapsed}",
                    ActionTypeText,
                    addressesHex,
                    sw.Elapsed);
            }

            // Step 13: Calculate final CP and update states
            var characterSheet = sheets.GetSheet<CharacterSheet>();
            var runeLevelBonusSheet = sheets.GetSheet<RuneLevelBonusSheet>();
            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();

            if (!characterSheet.TryGetValue(avatarState.characterId, out var myCharacterRow))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            // Calculate rune bonuses and options
            var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(
                runeStates,
                runeListSheet,
                runeLevelBonusSheet
            );

            var runeOptions = RuneHelper.GetRuneOptions(RuneInfos, runeStates, runeOptionSheet);

            // Calculate total CP including all modifiers
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

            // Update all states
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

            // Log total execution time
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Total elapsed: {Elapsed}",
                ActionTypeText,
                addressesHex,
                DateTimeOffset.UtcNow - started);

            return states;
        }
    }
}
