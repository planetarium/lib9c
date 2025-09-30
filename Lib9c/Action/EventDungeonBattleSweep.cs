using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Battle;
using Nekoyume.Exceptions;
using Nekoyume.Model.Arena;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Event;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using Nekoyume.TableData.Rune;
using Serilog;

namespace Nekoyume.Action
{
    /// <summary>
    /// Represents a sweep action that automatically completes multiple event dungeon battles without player interaction.
    ///
    /// <para>
    /// This action allows players to automatically repeat battles on already cleared event dungeon stages,
    /// consuming tickets and receiving rewards without manual combat interaction.
    /// </para>
    ///
    /// <para>
    /// Key Features:
    /// - Only works on previously cleared stages (no stage clearing)
    /// - No ticket purchase functionality (uses existing tickets only)
    /// - Automatic reward and experience calculation
    /// - Multiple battle execution in a single action
    /// </para>
    ///
    /// <para>
    /// This action modifies the following states:
    /// - AvatarState: Updates player's stats, inventory, and experience
    /// - EventDungeonInfo: Updates event dungeon progress and ticket usage
    /// - RuneState: Updates rune effects and bonuses
    /// - ItemSlotState: Updates equipped items
    /// - RuneSlotState: Updates equipped runes
    /// - CP: Updates character's combat power
    /// </para>
    ///
    /// <para>
    /// Execution Flow:
    /// 1. Validate input parameters and avatar state
    /// 2. Load and validate game sheets and configurations
    /// 3. Validate event dungeon info and ticket availability
    /// 4. Check stage clearance requirements
    /// 5. Update rune and item slot states
    /// 6. Calculate combat power (CP)
    /// 7. Generate rewards and experience
    /// 8. Update avatar state and return modified world state
    /// </para>
    ///
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2195
    /// </summary>
    [Serializable]
    [ActionType("event_dungeon_battle_sweep")]
    public class EventDungeonBattleSweep : GameAction, IEventDungeonBattleSweep
    {
        /// <summary>
        /// Maximum allowed play count per sweep action to prevent abuse and excessive resource consumption.
        /// </summary>
        private const int MaxPlayCount = 100;

        /// <summary>
        /// The address of the avatar performing the sweep action.
        /// </summary>
        public Address AvatarAddress;

        /// <summary>
        /// The ID of the event schedule for the dungeon.
        /// </summary>
        public int EventScheduleId;

        /// <summary>
        /// The ID of the event dungeon to sweep.
        /// </summary>
        public int EventDungeonId;

        /// <summary>
        /// The ID of the event dungeon stage to sweep.
        /// </summary>
        public int EventDungeonStageId;

        /// <summary>
        /// List of equipment item IDs to use during the sweep.
        /// </summary>
        public List<Guid> Equipments;

        /// <summary>
        /// List of costume item IDs to use during the sweep.
        /// </summary>
        public List<Guid> Costumes;

        /// <summary>
        /// List of food item IDs to use during the sweep.
        /// </summary>
        public List<Guid> Foods;

        /// <summary>
        /// List of rune slot information for equipped runes.
        /// </summary>
        public List<RuneSlotInfo> RuneInfos;

        /// <summary>
        /// The number of times to perform the sweep (must be between 1 and MaxPlayCount).
        /// </summary>
        public int PlayCount;

        Address IEventDungeonBattleSweep.AvatarAddress => AvatarAddress;
        int IEventDungeonBattleSweep.EventScheduleId => EventScheduleId;
        int IEventDungeonBattleSweep.EventDungeonId => EventDungeonId;
        int IEventDungeonBattleSweep.EventDungeonStageId => EventDungeonStageId;
        IEnumerable<Guid> IEventDungeonBattleSweep.Equipments => Equipments;
        IEnumerable<Guid> IEventDungeonBattleSweep.Costumes => Costumes;
        IEnumerable<Guid> IEventDungeonBattleSweep.Foods => Foods;
        IEnumerable<IValue> IEventDungeonBattleSweep.RuneSlotInfos =>
            RuneInfos.Select(x => x.Serialize());
        int IEventDungeonBattleSweep.PlayCount => PlayCount;

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
                    .Add(RuneInfos.OrderBy(x => x.SlotIndex).Select(x => x.Serialize())
                        .Serialize())
                    .Add(PlayCount.Serialize());

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
            RuneInfos = list[7].ToList(x => new RuneSlotInfo((List)x));
            PlayCount = list[8].ToInteger();
        }

        /// <summary>
        /// Executes the event dungeon battle sweep action.
        ///
        /// <para>
        /// This method performs the following steps:
        /// 1. Validates input parameters and loads avatar state
        /// 2. Loads and validates all required game sheets
        /// 3. Validates event dungeon configuration and ticket availability
        /// 4. Checks stage clearance requirements
        /// 5. Updates rune and item slot states
        /// 6. Calculates combat power (CP)
        /// 7. Generates rewards and experience
        /// 8. Updates all relevant states and returns the modified world
        /// </para>
        /// </summary>
        /// <param name="context">Action context containing previous state, signer, and block information</param>
        /// <returns>Modified world state with updated avatar, dungeon info, and other states</returns>
        public override IWorld Execute(IActionContext context)
        {
            // ========================================
            // STEP 1: INITIALIZATION AND VALIDATION
            // ========================================
            // This step initializes the action execution environment and validates basic parameters.
            // It includes gas consumption, address validation, and initial state loading.

            // Consume gas for action execution
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}EventDungeonBattleSweep exec started", addressesHex);

            // Validate play count - must be greater than 0 and within maximum limit
            if (PlayCount <= 0)
            {
                throw new PlayCountIsZeroException(
                    $"{addressesHex}playCount must be greater than 0. " +
                    $"current playCount : {PlayCount}");
            }

            if (PlayCount > MaxPlayCount)
            {
                throw new ExceedPlayCountException(
                    $"{addressesHex}playCount exceeds maximum allowed value. " +
                    $"Requested: {PlayCount}, Maximum: {MaxPlayCount}");
            }

            // Load avatar state - this contains player's character data, inventory, stats, etc.
            if (!states.TryGetAvatarState(
                    context.Signer,
                    AvatarAddress,
                    out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            // Check if collection state exists (for collection modifiers)
            var collectionExist = states.TryGetCollectionState(AvatarAddress, out var collectionState);

            // ========================================
            // STEP 2: LOAD AND VALIDATE GAME SHEETS
            // ========================================
            // This step loads all required game configuration sheets and validates their data.
            // It ensures all necessary game data is available for the sweep action.

            // Define all required sheet types for event dungeon battle sweep
            var sheetTypes = new List<Type>
            {
                // Event dungeon related sheets
                typeof(EventScheduleSheet),        // Event schedule configuration
                typeof(EventDungeonSheet),         // Event dungeon configuration
                typeof(EventDungeonStageSheet),    // Stage configuration
                typeof(EventDungeonStageWaveSheet), // Wave configuration

                // Battle and combat related sheets
                typeof(EnemySkillSheet),           // Enemy skill data
                typeof(CharacterSheet),            // Character base stats
                typeof(CharacterLevelSheet),       // Character level progression

                // Item and equipment related sheets
                typeof(CostumeStatSheet),          // Costume stat bonuses
                typeof(MaterialItemSheet),         // Material item data
                typeof(ItemRequirementSheet),      // Item requirement validation
                typeof(EquipmentItemRecipeSheet),  // Equipment crafting recipes
                typeof(EquipmentItemSubRecipeSheetV2), // Equipment sub-recipes
                typeof(EquipmentItemOptionSheet),  // Equipment option data

                // Rune related sheets
                typeof(RuneListSheet),             // Rune list and data
                typeof(RuneLevelBonusSheet),       // Rune level bonus calculation

                // Buff and skill related sheets
                typeof(BuffLimitSheet),            // Buff limit configuration
                typeof(BuffLinkSheet),             // Buff link configuration
            };

            // Add collection sheet if collection state exists
            if (collectionExist)
            {
                sheetTypes.Add(typeof(CollectionSheet));
            }

            // Load all required sheets from the world state
            var sheets = states.GetSheets(
                containSimulatorSheets: true,
                containValidateItemRequirementSheets: true,
                sheetTypes: sheetTypes);

            // ========================================
            // STEP 3: VALIDATE GAME CONFIGURATION AND ITEMS
            // ========================================

            // Validate event schedule - ensures the event is active and accessible
            var scheduleSheet = sheets.GetSheet<EventScheduleSheet>();
            var scheduleRow = scheduleSheet.ValidateFromActionForDungeon(
                context.BlockIndex,
                EventScheduleId,
                EventDungeonId,
                "event_dungeon_battle_sweep",
                addressesHex);

            // Validate event dungeon configuration
            var dungeonSheet = sheets.GetSheet<EventDungeonSheet>();
            var dungeonRow = dungeonSheet.ValidateFromAction(
                EventDungeonId,
                EventDungeonStageId,
                "event_dungeon_battle_sweep",
                addressesHex);

            // Validate event dungeon stage configuration
            var stageSheet = sheets.GetSheet<EventDungeonStageSheet>();
            var stageRow = stageSheet.ValidateFromAction(
                EventDungeonStageId,
                "event_dungeon_battle_sweep",
                addressesHex);

            // Get game configuration state for item validation
            var gameConfigState = states.GetGameConfigState();

            // Validate and load equipped items
            var equipmentList = avatarState.ValidateEquipmentsV3(
                Equipments, context.BlockIndex, gameConfigState);
            var costumeList = avatarState.ValidateCostumeV2(Costumes, gameConfigState);
            var foodIds = avatarState.ValidateConsumableV2(Foods, context.BlockIndex, gameConfigState);

            // Equip items to avatar state
            var equipmentAndCostumes = Equipments.Concat(Costumes);
            avatarState.EquipItems(equipmentAndCostumes);

            // Validate item requirements (level, stats, etc.)
            avatarState.ValidateItemRequirement(
                costumeList.Select(e => e.Id).Concat(foodIds).ToList(),
                equipmentList,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                addressesHex);

            // ========================================
            // STEP 4: VALIDATE EVENT DUNGEON INFO AND TICKETS
            // ========================================
            // This step validates the event dungeon information and ensures sufficient tickets
            // are available for the requested number of plays.

            // Get event dungeon info address and load the state
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(
                AvatarAddress,
                EventDungeonId);
            var eventDungeonInfo = states.GetLegacyState(eventDungeonInfoAddr)
                is Bencodex.Types.List serializedEventDungeonInfoList
                ? new EventDungeonInfo(serializedEventDungeonInfoList)
                : new EventDungeonInfo(remainingTickets: scheduleRow.DungeonTicketsMax);

            // Update ticket count based on time intervals
            // Tickets are reset periodically based on block intervals
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

            // Check if player has enough tickets for the requested play count
            // Note: Sweep does not support ticket purchase - only uses existing tickets
            if (!eventDungeonInfo.TryUseTickets(PlayCount))
            {
                throw new NotEnoughEventDungeonTicketsException(
                    "event_dungeon_battle_sweep",
                    addressesHex,
                    PlayCount,
                    eventDungeonInfo.RemainingTickets);
            }

            // Validate stage clearance requirement
            // Sweep can only be used on stages that have been previously cleared
            if (EventDungeonStageId != dungeonRow.StageBegin &&
                !eventDungeonInfo.IsCleared(EventDungeonStageId - 1))
            {
                throw new StageNotClearedException(
                    "event_dungeon_battle_sweep",
                    addressesHex,
                    EventDungeonStageId - 1,
                    eventDungeonInfo.ClearedStageId);
            }

            // ========================================
            // STEP 5: UPDATE SLOT STATES
            // ========================================
            // This step updates the rune and item slot states to reflect the equipped items
            // and their effects for the sweep action.

            // Update rune slot state - manages equipped runes and their effects
            var runeSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var runeSlotState = states.TryGetLegacyState(runeSlotStateAddress, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Adventure);
            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            runeSlotState.UpdateSlot(RuneInfos, runeListSheet);
            states = states.SetLegacyState(runeSlotStateAddress, runeSlotState.Serialize());

            // Update item slot state - manages equipped items and costumes
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var itemSlotState = states.TryGetLegacyState(itemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Adventure);
            itemSlotState.UpdateEquipment(Equipments);
            itemSlotState.UpdateCostumes(Costumes);
            states = states.SetLegacyState(itemSlotStateAddress, itemSlotState.Serialize());

            // ========================================
            // STEP 6: VALIDATE RUNES AND CALCULATE COMBAT POWER
            // ========================================

            // Get rune states and handle migration if needed
            var runeStates = states.GetRuneState(AvatarAddress, out var migrateRequired);
            if (migrateRequired)
            {
                states = states.SetRuneState(AvatarAddress, runeStates);
            }

            // Validate all equipped runes exist and are accessible
            foreach (var runeSlotInfo in RuneInfos)
            {
                runeStates.GetRuneState(runeSlotInfo.RuneId);
            }

            // Collect all equipped rune states for option calculation
            var equippedRune = new List<RuneState>();
            foreach (var runeInfo in runeSlotState.GetEquippedRuneSlotInfos())
            {
                if (runeStates.TryGetRuneState(runeInfo.RuneId, out var runeState))
                {
                    equippedRune.Add(runeState);
                }
            }

            // Calculate rune options based on equipped runes and their levels
            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
            var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            foreach (var runeState in equippedRune)
            {
                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var optionRow))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.RuneId);
                }

                if (!optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.Level);
                }

                runeOptions.Add(option);
            }

            // Calculate rune level bonus for additional stat bonuses
            var runeLevelBonusSheet = sheets.GetSheet<RuneLevelBonusSheet>();
            var runeLevelBonus =
                RuneHelper.CalculateRuneLevelBonus(runeStates, runeListSheet, runeLevelBonusSheet);

            // Get character base stats
            var characterSheet = sheets.GetSheet<CharacterSheet>();
            if (!characterSheet.TryGetValue(avatarState.characterId, out var characterRow))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            // Get collection modifiers if collection state exists
            var collectionModifiers = new List<StatModifier>();
            if (collectionExist)
            {
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
                collectionModifiers = collectionState.GetModifiers(collectionSheet);
            }

            // Calculate total combat power (CP) based on all equipped items, runes, and modifiers
            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            var cp = CPHelper.TotalCP(
                equipmentList, costumeList,
                runeOptions, avatarState.level,
                characterRow, costumeStatSheet, collectionModifiers, runeLevelBonus);

            // ========================================
            // STEP 7: GENERATE REWARDS AND EXPERIENCE
            // ========================================
            // This step generates rewards and experience based on the stage configuration
            // and the number of plays. It uses the same reward system as HackAndSlashSweep.

            // Generate reward items using random number generator
            var random = context.GetRandom();
            var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
            var rewardItems = GetRewardItems(
                random,
                PlayCount,
                stageRow,
                materialItemSheet);

            // Add reward items to avatar's inventory
            avatarState.UpdateInventory(rewardItems);

            // Calculate experience points based on stage and play count (same as EventDungeonBattle)
            var exp = scheduleRow.GetStageExp(
                EventDungeonStageId.ToEventDungeonStageNumber(),
                PlayCount);

            // Apply experience and handle level up using AvatarStateExtensions.UpdateExp
            var levelSheet = sheets.GetSheet<CharacterLevelSheet>();
            var (newLevel, newExp) = avatarState.GetLevelAndExp(levelSheet, EventDungeonStageId.ToEventDungeonStageNumber(), PlayCount);
            avatarState.UpdateExp(newLevel, newExp);

            // Note: Sweep does not clear stages - it only works on already cleared stages
            // This is a key difference from regular battle actions

            // ========================================
            // STEP 8: UPDATE STATES AND RETURN
            // ========================================
            // This final step updates all relevant states and returns the modified world state.
            // It includes updating avatar state, event dungeon info, and other related states.

            var ended = DateTimeOffset.UtcNow;
            Log.Debug(
                "{AddressesHex}EventDungeonBattleSweep Total Executed Time: {Elapsed}",
                addressesHex, ended - started
            );

            // Update all modified states and return the new world state
            return states
                .SetAvatarState(AvatarAddress, avatarState)                    // Update avatar with new stats, inventory, exp
                .SetLegacyState(eventDungeonInfoAddr, eventDungeonInfo.Serialize()) // Update event dungeon info with used tickets
                .SetCp(AvatarAddress, BattleType.Adventure, cp);               // Update combat power
        }

        /// <summary>
        /// Gets reward items for event dungeon sweep based on play count and stage configuration.
        ///
        /// <para>
        /// This method simulates multiple battle rewards without actually performing combat.
        /// It generates items based on the stage's drop configuration and the number of plays requested.
        /// </para>
        ///
        /// <para>
        /// Process:
        /// 1. Determine the number of items to drop per play (random between min and max)
        /// 2. For each play, generate rewards using the stage's item selector
        /// 3. Collect all rewards and sort them by item ID for consistency
        /// </para>
        /// </summary>
        /// <param name="random">Random number generator for deterministic reward generation</param>
        /// <param name="playCount">Number of plays to simulate (determines total reward count)</param>
        /// <param name="stageRow">Event dungeon stage configuration containing drop rates and item pools</param>
        /// <param name="materialItemSheet">Material item sheet for reward generation and validation</param>
        /// <returns>List of reward items sorted by item ID</returns>
        public static List<ItemBase> GetRewardItems(IRandom random,
            int playCount,
            EventDungeonStageSheet.Row stageRow,
            MaterialItemSheet materialItemSheet)
        {
            // Validate play count to prevent excessive resource consumption
            if (playCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playCount), "Play count must be greater than 0");
            }

            if (playCount > MaxPlayCount)
            {
                throw new ArgumentOutOfRangeException(nameof(playCount),
                    $"Play count exceeds maximum allowed value. Requested: {playCount}, Maximum: {MaxPlayCount}");
            }

            // Pre-allocate memory for expected reward items to improve performance
            var expectedMaxItems = playCount * stageRow.DropItemMax;
            var rewardItems = new List<ItemBase>(expectedMaxItems);

            // Determine how many items to drop per play (random between min and max)
            var maxCount = random.Next(stageRow.DropItemMin, stageRow.DropItemMax + 1);

            // Simulate rewards for each play count
            for (var i = 0; i < playCount; i++)
            {
                // Create item selector based on stage configuration
                var selector = StageSimulatorV1.SetItemSelector(stageRow, random);

                // Generate rewards for this play using the selector
                var rewards = Simulator.SetRewardV2(selector, maxCount, random,
                    materialItemSheet);

                // Add rewards to the total list
                rewardItems.AddRange(rewards);
            }

            // Sort rewards by item ID for consistent ordering
            rewardItems = rewardItems.OrderBy(x => x.Id).ToList();
            return rewardItems;
        }

    }
}
