using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Exceptions;
using Nekoyume.Model.Rune;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.Model.Item;
using Nekoyume.Module;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Represents a battle action in the infinite tower where players engage in combat with various conditions and restrictions.
    /// This action modifies the following states:
    /// - AvatarState: Updates player's stats, inventory, and experience
    /// - InfiniteTowerInfo: Updates tower progress and tickets
    /// - CollectionState: Updates collection progress
    /// - RuneState: Updates rune effects and bonuses
    /// - ItemSlotState: Updates equipped items
    /// - RuneSlotState: Updates equipped runes
    /// - CP: Updates character's combat power
    /// </summary>
    [Serializable]
    [ActionType("infinite_tower_battle")]
    public class InfiniteTowerBattle : GameAction, IInfiniteTowerBattleV1
    {
        /// <summary>
        /// The number of times this action can be played.
        /// </summary>
        public const int PlayCount = 1;

        /// <summary>
        /// The avatar address for the battle.
        /// </summary>
        public Address AvatarAddress;

        /// <summary>
        /// The infinite tower ID.
        /// </summary>
        public int InfiniteTowerId;

        /// <summary>
        /// The floor ID to challenge.
        /// </summary>
        public int FloorId;

        /// <summary>
        /// The equipment IDs to use in battle.
        /// </summary>
        public List<Guid> Equipments;

        /// <summary>
        /// The costume IDs to use in battle.
        /// </summary>
        public List<Guid> Costumes;

        /// <summary>
        /// The food IDs to use in battle.
        /// </summary>
        public List<Guid> Foods;

        /// <summary>
        /// The rune slot information for the battle.
        /// </summary>
        public List<RuneSlotInfo> RuneInfos;

        /// <summary>
        /// Whether to buy a ticket if needed.
        /// </summary>
        public bool BuyTicketIfNeeded;

        /// <summary>
        /// Whether to use NCG for buying tickets.
        /// </summary>
        public bool UseNcgForTicket;

        // IInfiniteTowerBattleV1 interface implementation
        Address IInfiniteTowerBattleV1.AvatarAddress => AvatarAddress;
        int IInfiniteTowerBattleV1.InfiniteTowerId => InfiniteTowerId;
        int IInfiniteTowerBattleV1.FloorId => FloorId;
        IEnumerable<Guid> IInfiniteTowerBattleV1.Equipments => Equipments;
        IEnumerable<Guid> IInfiniteTowerBattleV1.Costumes => Costumes;
        IEnumerable<Guid> IInfiniteTowerBattleV1.Foods => Foods;
        IEnumerable<IValue> IInfiniteTowerBattleV1.RuneSlotInfos =>
            RuneInfos.Select(x => x.Serialize());
        bool IInfiniteTowerBattleV1.BuyTicketIfNeeded => BuyTicketIfNeeded;

        /// <summary>
        /// Gets the plain value representation of this action for serialization.
        /// </summary>
        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var dict = new Dictionary<string, IValue>
                {
                    ["avatarAddress"] = AvatarAddress.Serialize(),
                    ["infiniteTowerId"] = InfiniteTowerId.Serialize(),
                    ["floorId"] = FloorId.Serialize(),
                    ["equipments"] = new List(Equipments.OrderBy(i => i).Select(e => e.Serialize())),
                    ["costumes"] = new List(Costumes.OrderBy(i => i).Select(e => e.Serialize())),
                    ["foods"] = new List(Foods.OrderBy(i => i).Select(e => e.Serialize())),
                    ["r"] = RuneInfos.OrderBy(x => x.SlotIndex).Select(x => x.Serialize()).Serialize(),
                    ["buyTicketIfNeeded"] = BuyTicketIfNeeded.Serialize(),
                    ["useNcgForTicket"] = UseNcgForTicket.Serialize(),
                };
                return dict.ToImmutableDictionary();
            }
        }

        /// <summary>
        /// Loads the action data from a plain value dictionary.
        /// </summary>
        /// <param name="plainValue">The plain value dictionary containing action data.</param>
        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            InfiniteTowerId = plainValue["infiniteTowerId"].ToInteger();
            FloorId = plainValue["floorId"].ToInteger();
            Equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            Costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            Foods = ((List)plainValue["foods"]).Select(e => e.ToGuid()).ToList();
            RuneInfos = plainValue["r"].ToList(x => new RuneSlotInfo((List)x));
            BuyTicketIfNeeded = plainValue["buyTicketIfNeeded"].ToBoolean();
            UseNcgForTicket = plainValue["useNcgForTicket"].ToBoolean();
        }

        /// <summary>
        /// Executes the infinite tower battle action.
        /// </summary>
        /// <param name="context">The action context containing state and block information.</param>
        /// <returns>The updated world state after the battle.</returns>
        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Verbose(
                "[InfiniteTowerBattle][{AddressesHex}] Execute() start",
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
                    "InfiniteTowerBattle",
                    addressesHex,
                    typeof(AvatarState),
                    AvatarAddress);
            }

            sw.Stop();
            Log.Verbose(
                "[InfiniteTowerBattle][{AddressesHex}] TryGetAvatarState: {Elapsed}",
                addressesHex,
                sw.Elapsed);

            // Get sheets
            sw.Restart();
            var collectionExist = states.TryGetCollectionState(AvatarAddress, out var collectionState);
            var sheetTypes = new List<Type>
            {
                typeof(InfiniteTowerFloorSheet),
                typeof(InfiniteTowerFloorWaveSheet),
                typeof(InfiniteTowerConditionSheet),
                typeof(InfiniteTowerScheduleSheet),
                typeof(EnemySkillSheet),
                typeof(SkillSheet),
                typeof(CostumeStatSheet),
                typeof(MaterialItemSheet),
                typeof(RuneListSheet),
                typeof(RuneLevelBonusSheet),
                typeof(BuffLimitSheet),
                typeof(BuffLinkSheet),
                typeof(CharacterSheet),
                typeof(ItemRequirementSheet),
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(EquipmentItemOptionSheet),
            };
            if (collectionExist)
            {
                sheetTypes.Add(typeof(CollectionSheet));
            }
            var sheets = states.GetSheets(
                containSimulatorSheets: true,
                containValidateItemRequirementSheets: true,
                containItemSheet: true,
                sheetTypes: sheetTypes
            );
            sw.Stop();
            Log.Verbose(
                "[InfiniteTowerBattle][{AddressesHex}] Get sheets: {Elapsed}",
                addressesHex,
                sw.Elapsed);

            // Validate fields
            sw.Restart();
            var floorSheet = sheets.GetSheet<InfiniteTowerFloorSheet>();
            if (!floorSheet.TryGetValue(FloorId, out var floorRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(floorSheet),
FloorId);
            }

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

            // Validate floor-specific restrictions
            floorRow.ValidateFloorRestrictions(equipmentList, costumeList);

            sw.Stop();
            Log.Verbose(
                "[InfiniteTowerBattle][{AddressesHex}] Validate fields: {Elapsed}",
                addressesHex,
                sw.Elapsed);

            // Validate avatar's infinite tower info
            sw.Restart();
            var infiniteTowerInfo = states.GetInfiniteTowerInfo(AvatarAddress, InfiniteTowerId);

            // Validate season consistency
            if (infiniteTowerInfo.InfiniteTowerId != InfiniteTowerId)
            {
                throw new InvalidOperationException($"Season mismatch: Expected {InfiniteTowerId}, but found {infiniteTowerInfo.InfiniteTowerId}");
            }

            // Get schedule configuration
            var scheduleSheet = sheets.GetSheet<InfiniteTowerScheduleSheet>();
            var scheduleRow = scheduleSheet.Values.FirstOrDefault(s => s.InfiniteTowerId == InfiniteTowerId);
            if (scheduleRow == null)
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(InfiniteTowerScheduleSheet),
                    InfiniteTowerId);
            }

            // Validate infinite tower ID
            scheduleRow.ValidateInfiniteTowerId(InfiniteTowerId, addressesHex);

            // Validate schedule timing
            scheduleRow.ValidateScheduleTiming(context.BlockIndex, InfiniteTowerId, addressesHex);

            // Validate floor range
            scheduleRow.ValidateFloorRange(FloorId, addressesHex);

            // Check if this is a new season (first time accessing this season)
            // Only reset if LastResetBlockIndex < StartBlockIndex (previous season's info or before season start)
            // If newly created (LastResetBlockIndex == 0) but current season is already active,
            // we should NOT reset, just refill tickets
            var lastResetBlockIndex = infiniteTowerInfo.LastResetBlockIndex;
            var isNewSeason = lastResetBlockIndex < scheduleRow.StartBlockIndex;

            if (isNewSeason)
            {
                infiniteTowerInfo.PerformSeasonReset(
                    context.BlockIndex,
                    scheduleRow.DailyFreeTickets,
                    scheduleRow.MaxTickets);

                Log.Verbose(
                    "[InfiniteTowerBattle][{AddressesHex}] Performed season reset at block {BlockIndex} (LastResetBlockIndex: {LastResetBlockIndex} -> {NewLastResetBlockIndex}, StartBlockIndex: {StartBlockIndex})",
                    addressesHex,
                    context.BlockIndex,
                    lastResetBlockIndex,
                    infiniteTowerInfo.LastResetBlockIndex,
                    scheduleRow.StartBlockIndex);
            }
            else
            {
                // Try to refill daily tickets (only during active season)
                // This will also initialize LastTicketRefillBlockIndex if it's 0
                if (infiniteTowerInfo.TryRefillDailyTickets(
                    scheduleRow.DailyFreeTickets,
                    scheduleRow.MaxTickets,
                    context.BlockIndex,
                    scheduleRow.ResetIntervalBlocks))
                {
                    Log.Verbose(
                        "[InfiniteTowerBattle][{AddressesHex}] Refilled daily tickets at block {BlockIndex}",
                        addressesHex,
                        context.BlockIndex);
                }
            }

            // Check if floor is accessible
            if (FloorId > 1 && !infiniteTowerInfo.IsCleared(FloorId - 1))
            {
                throw new StageNotClearedException(
                    "InfiniteTowerBattle",
                    addressesHex,
FloorId - 1,
                    infiniteTowerInfo.ClearedFloor);
            }

            // Check tickets
            if (!infiniteTowerInfo.TryUseTickets(PlayCount))
            {
                if (!BuyTicketIfNeeded)
                {
                    throw new NotEnoughInfiniteTowerTicketsException(
                        "InfiniteTowerBattle",
                        addressesHex,
                        PlayCount,
                        infiniteTowerInfo.RemainingTickets);
                }

                // Validate currency availability before purchase
                floorRow.ValidateCurrencyForTicketPurchase(context, states, AvatarAddress, UseNcgForTicket, addressesHex);

                // Purchase ticket with selected currency
                states = PurchaseTicket(context, states, floorRow, infiniteTowerInfo);

                // Ensure the purchased ticket is consumed for this play
                if (!infiniteTowerInfo.TryUseTickets(PlayCount))
                {
                    throw new InvalidOperationException(
                        $"[InfiniteTowerBattle][{addressesHex}] Ticket purchase did not result in usable tickets.");
                }
            }

            sw.Stop();
            Log.Verbose(
                "[InfiniteTowerBattle][{AddressesHex}] Validate infinite tower info: {Elapsed}",
                addressesHex,
                sw.Elapsed);

            // Update rune slot
            var runeSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var runeSlotState = states.TryGetLegacyState(runeSlotStateAddress, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Adventure);
            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            runeSlotState.UpdateSlot(RuneInfos, runeListSheet);
            states = states.SetLegacyState(runeSlotStateAddress, runeSlotState.Serialize());

            // Validate forbidden runes for this floor
            floorRow.ValidateRuneTypes(RuneInfos, runeListSheet);

            // Update item slot
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(AvatarAddress, BattleType.InfiniteTower);
            var itemSlotState = states.TryGetLegacyState(itemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.InfiniteTower);
            itemSlotState.UpdateEquipment(Equipments);
            itemSlotState.UpdateCostumes(Costumes);
            states = states.SetLegacyState(itemSlotStateAddress, itemSlotState.Serialize());

            // Validate equipment elemental type restrictions
            // Note: Equipment validation will be done in the simulator

            // Get conditions for this floor
            var conditionSheet = sheets.GetSheet<InfiniteTowerConditionSheet>();
            var guaranteedCondition = conditionSheet.Values
                .FirstOrDefault(c => c.Id == floorRow.GuaranteedConditionId);

            // Validate guaranteed condition exists
            if (guaranteedCondition == null && floorRow.GuaranteedConditionId > 0)
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(InfiniteTowerConditionSheet),
                    floorRow.GuaranteedConditionId);
            }

            // Get random instance once and reuse it
            var random = context.GetRandom();

            // Use weighted random conditions
            List<InfiniteTowerCondition> randomConditions;
            var weightedConditions = floorRow.GetRandomConditionsWithWeights();
            if (weightedConditions.Any())
            {
                randomConditions = floorRow.GetRandomConditionsWithWeights(
                    conditionSheet,
                    random,
                    guaranteedCondition?.Id);
            }
            else
            {
                // If no weighted conditions are specified, select from all available conditions
                randomConditions = floorRow.GetRandomConditions(
                    conditionSheet,
                    random,
                    guaranteedCondition?.Id);
            }

            // Validate random conditions count
            if (randomConditions.Count < floorRow.MinRandomConditions ||
                randomConditions.Count > floorRow.MaxRandomConditions)
            {
                throw new InvalidOperationException(
                    $"Random conditions count mismatch: Expected {floorRow.MinRandomConditions}-{floorRow.MaxRandomConditions}, got {randomConditions.Count}");
            }

            var allConditions = new List<InfiniteTowerCondition>();
            if (guaranteedCondition != null)
            {
                allConditions.Add(new InfiniteTowerCondition(guaranteedCondition));
            }
            allConditions.AddRange(randomConditions);

            // Validate no duplicate conditions
            var conditionIds = allConditions.Select(c => c.Id).ToList();
            if (conditionIds.Count != conditionIds.Distinct().Count())
            {
                var duplicateIds = conditionIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                throw new InvalidOperationException($"Duplicate conditions detected in floor conditions: {string.Join(", ", duplicateIds)}");
            }

            // Conditions will be applied in the simulator to CharacterStats
            // No need to modify equipment or runes directly

            // Simulate
            sw.Restart();
            var simulatorSheets = sheets.GetSimulatorSheets();
            var runeStates = states.GetRuneState(AvatarAddress, out var migrateRequired2);
            if (migrateRequired2)
            {
                states = states.SetRuneState(AvatarAddress, runeStates);
            }

            var collectionModifiers = new List<StatModifier>();
            if (collectionExist)
            {
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
                collectionModifiers = collectionState.GetModifiers(collectionSheet);
            }

            var buffLimitSheet = sheets.GetSheet<BuffLimitSheet>();
            var buffLinkSheet = sheets.GetSheet<BuffLinkSheet>();
            var floorWaveSheet = sheets.GetSheet<InfiniteTowerFloorWaveSheet>();
            var floorWaveRow = floorWaveSheet[FloorId];
            var waveRows = floorWaveRow.Waves;

            // Calculate and validate CP before simulation & rewards
            var characterSheet = sheets.GetSheet<CharacterSheet>();
            var runeLevelBonusSheet = sheets.GetSheet<RuneLevelBonusSheet>();
            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            if (!characterSheet.TryGetValue(avatarState.characterId, out var myCharacterRow))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }
            var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(
                runeStates, runeListSheet, runeLevelBonusSheet
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
            floorRow.ValidateCpRequirements(cp);

            var simulator = new InfiniteTowerSimulator(
                random,
                avatarState,
                Foods,
                runeStates,
                runeSlotState,
                InfiniteTowerId,
                FloorId,
                floorRow,
                waveRows,
                infiniteTowerInfo.IsCleared(FloorId),
                0, // No experience reward
                simulatorSheets,
                sheets.GetSheet<EnemySkillSheet>(),
                sheets.GetSheet<CostumeStatSheet>(),
                sheets.GetItemSheet(),
                collectionModifiers,
                buffLimitSheet,
                buffLinkSheet,
                allConditions,
                (int)gameConfigState.ShatterStrikeMaxDamage,
                logEvent: true); // Turn limit for all waves combined

            simulator.Simulate();
            sw.Stop();
            Log.Verbose(
                "[InfiniteTowerBattle][{AddressesHex}] Simulate: {Elapsed}",
                addressesHex,
                sw.Elapsed);

            // Update avatar's infinite tower info and process rewards
            if (simulator.Log.IsClear)
            {
                sw.Restart();
                var wasAlreadyCleared = infiniteTowerInfo.IsCleared(FloorId);
                infiniteTowerInfo.ClearFloor(FloorId);
                sw.Stop();
                Log.Verbose(
                    "[InfiniteTowerBattle][{AddressesHex}] Update infinite tower info: {Elapsed}",
                    addressesHex,
                    sw.Elapsed);

                // Process rewards only for first-time floor completion
                if (!wasAlreadyCleared)
                {
                    sw.Restart();
                    states = floorRow.ProcessRewards(context, states, avatarState, AvatarAddress);
                    sw.Stop();
                    Log.Verbose(
                        "[InfiniteTowerBattle][{AddressesHex}] Process rewards (first clear): {Elapsed}",
                        addressesHex,
                        sw.Elapsed);
                }
                else
                {
                    Log.Verbose(
                        "[InfiniteTowerBattle][{AddressesHex}] Floor {FloorId} already cleared, skipping rewards",
                        addressesHex,
                        FloorId);
                }

                // Update infinite tower board state for clear count tracking (only for first-time clears)
                if (!wasAlreadyCleared)
                {
                    sw.Restart();
                    states = UpdateInfiniteTowerBoardState(states, context, addressesHex);
                    sw.Stop();
                    Log.Verbose(
                        "[InfiniteTowerBattle][{AddressesHex}] Update infinite tower board state (first clear): {Elapsed}",
                        addressesHex,
                        sw.Elapsed);
                }
                else
                {
                    Log.Verbose(
                        "[InfiniteTowerBattle][{AddressesHex}] Floor {FloorId} already cleared, skipping board state update",
                        addressesHex,
                        FloorId);
                }
            }

            // Set states
            sw.Restart();
            states = states
                .SetAvatarState(AvatarAddress, avatarState)
                .SetInfiniteTowerInfo(AvatarAddress, infiniteTowerInfo)
                .SetCp(AvatarAddress, BattleType.InfiniteTower, cp);

            sw.Stop();
            Log.Verbose(
                "[InfiniteTowerBattle][{AddressesHex}] Set states: {Elapsed}",
                addressesHex,
                sw.Elapsed);

            Log.Verbose(
                "[InfiniteTowerBattle][{AddressesHex}] Total elapsed: {Elapsed}",
                addressesHex,
                DateTimeOffset.UtcNow - started);
            return states;
        }


        /// <summary>
        /// Purchases a ticket using the selected currency (NCG or Material).
        /// </summary>
        /// <param name="context">The action context.</param>
        /// <param name="states">The world state.</param>
        /// <param name="floorRow">The floor configuration.</param>
        /// <param name="infiniteTowerInfo">The infinite tower info to update.</param>
        /// <returns>Updated world state.</returns>
        private IWorld PurchaseTicket(
            IActionContext context,
            IWorld states,
            InfiniteTowerFloorSheet.Row floorRow,
            InfiniteTowerInfo infiniteTowerInfo)
        {
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);

            if (UseNcgForTicket)
            {
                if (!floorRow.NcgCost.HasValue)
                {
                    throw new InvalidOperationException(
                        $"[InfiniteTowerBattle][{addressesHex}] NCG cost is not configured for this floor");
                }

                // Purchase with NCG
                var goldCurrency = states.GetGoldCurrency();
                var ticketCost = goldCurrency * floorRow.NcgCost.Value;

                // Check if player has enough NCG
                var goldBalance = states.GetBalance(context.Signer, goldCurrency);
                if (goldBalance < ticketCost)
                {
                    throw new InsufficientBalanceException(
                        $"[InfiniteTowerBattle][{addressesHex}] Insufficient NCG balance. Required: {ticketCost}, Available: {goldBalance}",
                        context.Signer,
                        goldBalance);
                }

                // Transfer NCG to fee address
                var feeAddress = states.GetFeeAddress(context.BlockIndex);
                states = states.TransferAsset(context, context.Signer, feeAddress, ticketCost);

                Log.Verbose(
                    "[InfiniteTowerBattle][{AddressesHex}] Purchased ticket with NCG: {Cost}",
                    addressesHex,
                    ticketCost);
            }
            else
            {
                // Purchase with material (inventory item)
                var materialSheet = states.GetSheet<MaterialItemSheet>();
                var materialRow = materialSheet.OrderedList.First(m => m.Id == floorRow.MaterialCostId);

                // Get avatar's inventory
                var inventory = states.GetInventoryV2(AvatarAddress);

                // Check if player has enough material in inventory
                if (!inventory.RemoveFungibleItem(materialRow.ItemId, context.BlockIndex, floorRow.MaterialCostCount.Value))
                {
                    throw new NotEnoughMaterialException(
                        $"[InfiniteTowerBattle][{addressesHex}] Not enough material to purchase ticket: needs {floorRow.MaterialCostCount}");
                }

                // Update inventory
                states = states.SetInventory(AvatarAddress, inventory);

                Log.Verbose(
                    "[InfiniteTowerBattle][{AddressesHex}] Purchased ticket with Material: {MaterialId} x {Count}",
                    addressesHex,
                    floorRow.MaterialCostId,
                    floorRow.MaterialCostCount);
            }

            // Add ticket to infinite tower info
            infiniteTowerInfo.AddTickets(1);
            infiniteTowerInfo.IncreaseNumberOfTicketPurchases();

            Log.Verbose(
                "[InfiniteTowerBattle][{AddressesHex}] Added ticket. Remaining tickets: {Tickets}",
                addressesHex,
                infiniteTowerInfo.RemainingTickets);

            return states;
        }

        /// <summary>
        /// Helper method to manually trigger daily ticket refill for testing purposes.
        /// </summary>
        public static void RefillDailyTickets(
            IWorld states,
            Address avatarAddress,
            int infiniteTowerId,
            long currentBlockIndex)
        {
            var infiniteTowerInfo = states.GetInfiniteTowerInfo(avatarAddress, infiniteTowerId);

            // Get schedule configuration
            var sheets = states.GetSheets(sheetTypes: new[] { typeof(InfiniteTowerScheduleSheet) });
            var scheduleSheet = sheets.GetSheet<InfiniteTowerScheduleSheet>();
            var scheduleRow = scheduleSheet.Values.FirstOrDefault(s => s.InfiniteTowerId == infiniteTowerId);

            if (scheduleRow != null)
            {
                infiniteTowerInfo.TryRefillDailyTickets(
                    scheduleRow.DailyFreeTickets,
                    scheduleRow.MaxTickets,
                    currentBlockIndex);

                // Note: states parameter is not returned, this is for testing purposes only
                _ = states.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);
            }
        }

        /// <summary>
        /// Helper method to manually trigger full reset for testing purposes.
        /// </summary>
        public static void PerformFullReset(
            IWorld states,
            Address avatarAddress,
            int infiniteTowerId,
            long currentBlockIndex)
        {
            var infiniteTowerInfo = states.GetInfiniteTowerInfo(avatarAddress, infiniteTowerId);

            // Get schedule configuration
            var sheets = states.GetSheets(sheetTypes: new[] { typeof(InfiniteTowerScheduleSheet) });
            var scheduleSheet = sheets.GetSheet<InfiniteTowerScheduleSheet>();
            var scheduleRow = scheduleSheet.Values.FirstOrDefault(s => s.InfiniteTowerId == infiniteTowerId);

            if (scheduleRow != null)
            {
                infiniteTowerInfo.PerformSeasonReset(
                    currentBlockIndex,
                    scheduleRow.DailyFreeTickets,
                    scheduleRow.MaxTickets);

                // Note: states parameter is not returned, this is for testing purposes only
                _ = states.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);
            }
        }


        /// <summary>
        /// Updates the infinite tower board state to track floor clear counts.
        /// </summary>
        private IWorld UpdateInfiniteTowerBoardState(
            IWorld states,
            IActionContext context,
            string addressesHex)
        {
            Log.Verbose(
                "[InfiniteTowerBattle][{AddressesHex}] Recorded floor {FloorId} clear for infinite tower {InfiniteTowerId}",
                addressesHex,
                FloorId,
                InfiniteTowerId);

            return states.RecordFloorClear(InfiniteTowerId, FloorId, context.BlockIndex);
        }



    }
}
