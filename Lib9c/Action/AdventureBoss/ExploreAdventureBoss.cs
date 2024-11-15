using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Battle;
using Nekoyume.Battle.AdventureBoss;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.AdventureBoss;
using Nekoyume.TableData.Rune;

namespace Nekoyume.Action.AdventureBoss
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class ExploreAdventureBoss : GameAction
    {
        public const string TypeIdentifier = "explore_adventure_boss";

        public int Season;
        public Address AvatarAddress;
        public List<Guid> Costumes;
        public List<Guid> Equipments;
        public List<Guid> Foods;
        public List<RuneSlotInfo> RuneInfos;
        public int? StageBuffId;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var dict = new Dictionary<string, IValue>
                {
                    ["season"] = (Integer)Season,
                    ["avatarAddress"] = AvatarAddress.Serialize(),
                    ["costumes"] = new List(Costumes.OrderBy(i => i).Select(e => e.Serialize())),
                    ["equipments"] =
                        new List(Equipments.OrderBy(i => i).Select(e => e.Serialize())),
                    ["r"] = RuneInfos.OrderBy(x => x.SlotIndex).Select(x => x.Serialize())
                        .Serialize(),
                    ["foods"] = new List(Foods.OrderBy(i => i).Select(e => e.Serialize())),
                };
                if (StageBuffId.HasValue)
                {
                    dict["stageBuffId"] = StageBuffId.Serialize();
                }

                return dict.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            Season = (Integer)plainValue["season"];
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            Costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            Equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            Foods = ((List)plainValue["foods"]).Select(e => e.ToGuid()).ToList();
            RuneInfos = plainValue["r"].ToList(x => new RuneSlotInfo((List)x));

            if (plainValue.ContainsKey("stageBuffId"))
            {
                StageBuffId = plainValue["stageBuffId"].ToNullableInteger();
            }
        }


        public override IWorld Execute(IActionContext context)
        {
            var addressesHex = $"[{context.Signer.ToHex()}, {AvatarAddress.ToHex()}]";
            GasTracer.UseGas(1);
            var states = context.PreviousState;

            // Validation
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

            if (!states.TryGetAvatarState(context.Signer, AvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var exploreBoard = states.GetExploreBoard(Season);
            Explorer explorer;
            if (states.TryGetExplorer(Season, AvatarAddress, out var exp))
            {
                explorer = exp;
            }
            else
            {
                explorer = new Explorer(AvatarAddress, avatarState.name);
                var explorerList = states.GetExplorerList(Season);
                explorerList.AddExplorer(AvatarAddress, avatarState.name);
                exploreBoard.ExplorerCount = explorerList.Explorers.Count;
                states = states.SetExplorerList(Season, explorerList);
            }

            if (explorer.Floor == explorer.MaxFloor)
            {
                throw new InvalidOperationException("Reached to locked floor. Unlock floor first.");
            }

            if (explorer.Floor == UnlockFloor.TotalFloor)
            {
                throw new InvalidOperationException("Already cleared all floors");
            }

            var sheets = states.GetSheets(
                containSimulatorSheets: true,
                sheetTypes: new[]
                {
                    typeof(AdventureBossSheet),
                    typeof(AdventureBossFloorSheet),
                    typeof(AdventureBossFloorWaveSheet),
                    typeof(CollectionSheet),
                    typeof(EnemySkillSheet),
                    typeof(CostumeStatSheet),
                    typeof(BuffLimitSheet),
                    typeof(BuffLinkSheet),
                    typeof(ItemRequirementSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                    typeof(RuneListSheet),
                    typeof(RuneLevelBonusSheet),
                });
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();
            var material =
                materialSheet.OrderedList.First(row => row.ItemSubType == ItemSubType.ApStone);
            var random = context.GetRandom();
            var selector = new WeightedSelector<AdventureBossFloorSheet.RewardData>(random);
            var rewardList = new List<AdventureBossSheet.RewardAmountData>();

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

            // Get data for simulator
            var runeStates = states.GetRuneState(AvatarAddress, out var migrateRequired);
            // Passive migrate runeStates
            if (migrateRequired)
            {
                states = states.SetRuneState(AvatarAddress, runeStates);
            }

            var collectionExist =
                states.TryGetCollectionState(AvatarAddress, out var collectionState) &&
                collectionState.Ids.Any();
            var collectionModifiers = new List<StatModifier>();
            if (collectionExist)
            {
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
                collectionModifiers = collectionState.GetModifiers(collectionSheet);
            }

            var floorSheet = sheets.GetSheet<AdventureBossFloorSheet>();
            var floorWaveSheet = sheets.GetSheet<AdventureBossFloorWaveSheet>();
            var simulatorSheets = sheets.GetSimulatorSheets();
            var enemySkillSheet = sheets.GetSheet<EnemySkillSheet>();
            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
            var buffLimitSheet = sheets.GetSheet<BuffLimitSheet>();
            var buffLinkSheet = sheets.GetSheet<BuffLinkSheet>();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the game config state was failed to load.");
            }

            var bossId = states.GetSheet<AdventureBossSheet>().Values
                .First(row => row.BossId == latestSeason.BossId).Id;
            var floorRows = states.GetSheet<AdventureBossFloorSheet>().Values
                .Where(row => row.AdventureBossId == bossId).ToList();
            var firstRewardSheet = states.GetSheet<AdventureBossFloorFirstRewardSheet>();
            var pointSheet = states.GetSheet<AdventureBossFloorPointSheet>();

            AdventureBossSimulator simulator = null;
            var firstFloorId = 0;
            var floorIdList = new List<int>();

            // Claim floors from last failed
            var exploreAp = sheets.GetSheet<AdventureBossSheet>().OrderedList
                .First(row => row.BossId == latestSeason.BossId).ExploreAp;
            for (var fl = explorer.Floor + 1; fl < explorer.MaxFloor + 1; fl++)
            {
                // Get Data for simulator
                var floorRow = floorRows.First(row => row.Floor == fl);
                if (!floorSheet.TryGetValue(fl, out var flRow))
                {
                    throw new SheetRowNotFoundException(addressesHex, nameof(floorSheet), fl);
                }

                if (firstFloorId == 0)
                {
                    firstFloorId = floorRow.Id;
                }

                var rewards =
                    AdventureBossSimulator.GetWaveRewards(random, flRow, materialItemSheet);

                // Use AP Potion
                if (!avatarState.inventory.RemoveFungibleItem(material.ItemId, context.BlockIndex,
                        exploreAp))
                {
                    break;
                }

                exploreBoard.UsedApPotion += exploreAp;
                explorer.UsedApPotion += exploreAp;

                simulator = new AdventureBossSimulator(
                    bossId: latestSeason.BossId,
                    floorId: floorRow.Id,
                    random,
                    avatarState,
                    floorRow.Id == firstFloorId ? Foods : new List<Guid>(),
                    runeStates,
                    runeSlotState,
                    floorRow,
                    floorWaveSheet[floorRow.Id],
                    simulatorSheets,
                    enemySkillSheet,
                    costumeStatSheet,
                    rewards,
                    collectionModifiers,
                    buffLimitSheet,
                    buffLinkSheet,
                    false,
                    gameConfigState.ShatterStrikeMaxDamage
                );

                simulator.Simulate();

                // Get Reward if cleared
                if (simulator.Log.IsClear)
                {
                    // Add point, reward
                    var pointRow = pointSheet[floorRow.Id];
                    var point = random.Next(pointRow.MinPoint, pointRow.MaxPoint + 1);

                    explorer.Floor = fl;
                    explorer.Score += point;

                    exploreBoard.TotalPoint += point;

                    var firstReward = firstRewardSheet[floorRow.Id];
                    foreach (var reward in firstReward.Rewards)
                    {
                        rewardList.Add(new AdventureBossSheet.RewardAmountData(
                            reward.ItemType, reward.ItemId, reward.Amount
                        ));
                    }

                    selector.Clear();
                    foreach (var reward in floorRow.Rewards)
                    {
                        selector.Add(reward, reward.Ratio);
                    }

                    var selected = selector.Select(1).First();
                    rewardList.Add(new AdventureBossSheet.RewardAmountData(
                        selected.ItemType,
                        selected.ItemId,
                        random.Next(selected.Min, selected.Max + 1))
                    );

                    // Add floorId for breakthrough
                    if (fl < explorer.MaxFloor + 1)
                    {
                        floorIdList.Add(floorRow.Id);
                    }
                }
                else
                {
                    break;
                }
            }

            if (simulator is not null && simulator.LogEvent && floorIdList.Count > 0)
            {
                simulator.AddBreakthrough(floorIdList, floorWaveSheet);
            }

            states = AdventureBossHelper.AddExploreRewards(
                context, states, AvatarAddress, avatarState.inventory, rewardList
            );

            return states
                .SetInventory(AvatarAddress, avatarState.inventory)
                .SetExploreBoard(Season, exploreBoard)
                .SetExplorer(Season, explorer);
        }
    }
}
