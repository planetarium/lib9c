using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Action.Exceptions.AdventureBoss;
using Lib9c.Battle;
using Lib9c.Battle.AdventureBoss;
using Lib9c.Exceptions;
using Lib9c.Extensions;
using Lib9c.Helper;
using Lib9c.Model.AdventureBoss;
using Lib9c.Model.Arena;
using Lib9c.Model.EnumType;
using Lib9c.Model.Item;
using Lib9c.Model.Stat;
using Lib9c.Model.State;
using Lib9c.Module;
using Lib9c.TableData;
using Lib9c.TableData.AdventureBoss;
using Lib9c.TableData.Character;
using Lib9c.TableData.Item;
using Lib9c.TableData.Rune;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Action.AdventureBoss
{
    /// <summary>
    /// Represents a sweep action that automatically completes multiple adventure boss stages without player interaction.
    /// This action modifies the following states:
    /// - AvatarState: Updates player's stats, inventory, and experience
    /// - ExploreBoard: Updates exploration progress and points
    /// - Explorer: Updates player's exploration status and achievements
    /// - CollectionState: Updates collection progress
    /// - RuneState: Updates rune effects and bonuses
    /// - ItemSlotState: Updates equipped items
    /// - RuneSlotState: Updates equipped runes
    /// - CP: Updates character's combat power
    /// </summary>
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class SweepAdventureBoss : GameAction
    {
        public const string TypeIdentifier = "sweep_adventure_boss";

        public int Season;
        public Address AvatarAddress;
        public List<Guid> Costumes;
        public List<Guid> Equipments;
        public List<RuneSlotInfo> RuneInfos;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["s"] = (Integer)Season,
                ["a"] = AvatarAddress.Serialize(),
                ["c"] = new List(Costumes.OrderBy(i => i).Select(e => e.Serialize())),
                ["e"] =
                    new List(Equipments.OrderBy(i => i).Select(e => e.Serialize())),
                ["r"] = RuneInfos.OrderBy(x => x.SlotIndex).Select(x => x.Serialize())
                    .Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            Season = (Integer)plainValue["s"];
            AvatarAddress = plainValue["a"].ToAddress();
            Costumes = ((List)plainValue["c"]).Select(e => e.ToGuid()).ToList();
            Equipments = ((List)plainValue["e"]).Select(e => e.ToGuid()).ToList();
            RuneInfos = plainValue["r"].ToList(x => new RuneSlotInfo((List)x));
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var states = context.PreviousState;

            // Validation
            var addresses = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            // NOTE: The `AvatarAddress` must contained in `Signer`'s `AgentState.avatarAddresses`.
            if (!Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress))
            {
                throw new InvalidActionFieldException(
                    TypeIdentifier,
                    addresses,
                    nameof(AvatarAddress),
                    $"Signer({context.Signer}) is not contained in" +
                    $" AvatarAddress({AvatarAddress}).");
            }

            var latestSeason = states.GetLatestAdventureBossSeason();
            if (latestSeason.Season != Season)
            {
                throw new InvalidAdventureBossSeasonException(
                    $"Given season {Season} is not current season: {latestSeason.Season}"
                );
            }

            if (context.BlockIndex > latestSeason.EndBlockIndex)
            {
                throw new InvalidSeasonException(
                    $"Season finished at block {latestSeason.EndBlockIndex}."
                );
            }

            var avatarState = states.GetAvatarState(AvatarAddress, true, false, false);
            if (avatarState is null || !avatarState.agentAddress.Equals(context.Signer))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var exploreBoard = states.GetExploreBoard(Season);
            var explorer = states.TryGetExplorer(Season, AvatarAddress, out var exp)
                ? exp
                : new Explorer(AvatarAddress, avatarState.name);

            if (explorer.Floor == 0)
            {
                throw new InvalidOperationException("Cannot sweep without cleared stage.");
            }

            // Use AP Potions
            var unitSweepAp = states.GetSheet<AdventureBossSheet>().OrderedList
                .First(row => row.BossId == latestSeason.BossId).SweepAp;
            var requiredPotion = explorer.Floor * unitSweepAp;
            var sheets = states.GetSheets(
                containSimulatorSheets: true,
                sheetTypes: new[]
                {
                    typeof(CharacterSheet),
                    typeof(CollectionSheet),
                    typeof(CostumeStatSheet),
                    typeof(ItemRequirementSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                    typeof(MaterialItemSheet),
                    typeof(RuneListSheet),
                    typeof(RuneOptionSheet),
                    typeof(RuneLevelBonusSheet),
                    typeof(AdventureBossFloorWaveSheet),
                });
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();
            var material =
                materialSheet.OrderedList.First(row => row.ItemSubType == ItemSubType.ApStone);
            var inventory = states.GetInventoryV2(AvatarAddress);
            if (!inventory.RemoveFungibleItem(material.ItemId, context.BlockIndex, requiredPotion))
            {
                throw new NotEnoughMaterialException(
                    $"{requiredPotion} AP potions needed. You only have {inventory.Items.FirstOrDefault(item => item.item.ItemSubType == ItemSubType.ApStone)?.count}"
                );
            }

            // Validate
            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the game config state was failed to load.");
            }

            var equipmentList =
                avatarState.ValidateEquipmentsV3(Equipments, context.BlockIndex, gameConfigState);
            var costumeList = avatarState.ValidateCostumeV2(Costumes, gameConfigState);
            var items = Equipments.Concat(Costumes);
            avatarState.EquipItems(items);
            avatarState.ValidateItemRequirement(
                costumeList.Select(e => e.Id).ToList(),
                equipmentList,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                addressesHex);

            // update rune slot
            var runeSlotStateAddress =
                RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var runeSlotState =
                states.TryGetLegacyState(runeSlotStateAddress, out List rawRuneSlotState)
                    ? new RuneSlotState(rawRuneSlotState)
                    : new RuneSlotState(BattleType.Adventure);
            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            runeSlotState.UpdateSlot(RuneInfos, runeListSheet);
            states = states.SetLegacyState(runeSlotStateAddress, runeSlotState.Serialize());

            // update item slot
            var itemSlotStateAddress =
                ItemSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var itemSlotState =
                states.TryGetLegacyState(itemSlotStateAddress, out List rawItemSlotState)
                    ? new ItemSlotState(rawItemSlotState)
                    : new ItemSlotState(BattleType.Adventure);
            itemSlotState.UpdateEquipment(Equipments);
            itemSlotState.UpdateCostumes(Costumes);
            states = states.SetLegacyState(itemSlotStateAddress, itemSlotState.Serialize());

            exploreBoard.UsedApPotion += requiredPotion;
            explorer.UsedApPotion += requiredPotion;

            var adventureBossId = states.GetSheet<AdventureBossSheet>().OrderedList
                .First(row => row.BossId == latestSeason.BossId).Id;
            var floorRows = states.GetSheet<AdventureBossFloorSheet>().OrderedList
                .Where(row => row.AdventureBossId == adventureBossId).ToList();
            var floorId = floorRows.First(r => r.Floor == explorer.Floor).Id;

            var simulator = new AdventureBossSimulator(
                latestSeason.BossId, floorId, context.GetRandom(),
                avatarState, sheets.GetSimulatorSheets(), logEvent: false
            );
            var floorIdList = new List<int>();
            for (var fl = 1; fl <= explorer.MaxFloor; fl++)
            {
                floorIdList.Add(floorRows.First(row => row.Floor == fl).Id);
            }

            simulator.AddBreakthrough(floorIdList, sheets.GetSheet<AdventureBossFloorWaveSheet>());

            // Add point, reward
            var point = 0;
            var rewardList = new List<AdventureBossSheet.RewardAmountData>();
            var random = context.GetRandom();
            var selector = new WeightedSelector<AdventureBossFloorSheet.RewardData>(random);
            var bossId = states.GetSheet<AdventureBossSheet>().Values
                .First(row => row.BossId == latestSeason.BossId).Id;
            var floorPointSheet = states.GetSheet<AdventureBossFloorPointSheet>();
            var floorSheet = states.GetSheet<AdventureBossFloorSheet>();
            for (var fl = 1; fl <= explorer.Floor; fl++)
            {
                var floorRow = floorSheet.Values.Where(row => row.AdventureBossId == bossId)
                    .First(row => row.Floor == fl);
                var pointRow = floorPointSheet[floorRow.Id];
                point += random.Next(pointRow.MinPoint, pointRow.MaxPoint + 1);

                selector.Clear();
                foreach (var reward in floorRow.Rewards)
                {
                    selector.Add(reward, reward.Ratio);
                }

                var selected = selector.Select(1).First();
                rewardList.Add(new AdventureBossSheet.RewardAmountData(
                    selected.ItemType,
                    selected.ItemId,
                    random.Next(selected.Min, selected.Max + 1)
                ));
            }

            exploreBoard.TotalPoint += point;
            explorer.Score += point;
            states = AdventureBossHelper.AddExploreRewards(
                context, states, AvatarAddress, inventory, rewardList
            );

            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            var characterSheet = sheets.GetSheet<CharacterSheet>();
            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
            var runeLevelBonusSheet = sheets.GetSheet<RuneLevelBonusSheet>();
            if (!characterSheet.TryGetValue(avatarState.characterId, out var myCharacterRow))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var runeStates = states.GetRuneState(AvatarAddress, out var migrateRequired);
            // Passive migrate runeStates
            if (migrateRequired)
            {
                states = states.SetRuneState(AvatarAddress, runeStates);
            }

            // just validate
            foreach (var runeSlotInfo in RuneInfos)
            {
                runeStates.GetRuneState(runeSlotInfo.RuneId);
            }

            var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(
                runeStates,
                runeListSheet,
                runeLevelBonusSheet
            );

            var runeOptions = RuneHelper.GetRuneOptions(RuneInfos, runeStates, runeOptionSheet);

            var collectionExist =
                states.TryGetCollectionState(AvatarAddress, out var collectionState) &&
                collectionState.Ids.Any();
            var collectionModifiers = new List<StatModifier>();
            if (collectionExist)
            {
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
                collectionModifiers = collectionState.GetModifiers(collectionSheet);
            }

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

            return states
                .SetInventory(AvatarAddress, inventory)
                .SetExploreBoard(Season, exploreBoard)
                .SetExplorer(Season, explorer)
                .SetCp(AvatarAddress, BattleType.Adventure, cp);
        }
    }
}
