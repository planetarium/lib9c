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
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.AdventureBoss;
using Nekoyume.TableData.Rune;

namespace Nekoyume.Action.AdventureBoss
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class SweepAdventureBoss : GameAction
    {
        public const string TypeIdentifier = "sweep_adventure_boss";

        public const int UnitApPotion = 2;

        public int Season;
        public Address AvatarAddress;
        public List<Guid> Costumes;
        public List<Guid> Equipments;
        public List<RuneSlotInfo> RuneInfos;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["season"] = (Integer)Season,
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["costumes"] = new List(Costumes.OrderBy(i => i).Select(e => e.Serialize())),
                ["equipments"] =
                    new List(Equipments.OrderBy(i => i).Select(e => e.Serialize())),
                ["r"] = RuneInfos.OrderBy(x => x.SlotIndex).Select(x => x.Serialize())
                    .Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            Season = (Integer)plainValue["season"];
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            Costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            Equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            RuneInfos = plainValue["r"].ToList(x => new RuneSlotInfo((List)x));
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
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

            var avatarState = states.GetAvatarState(AvatarAddress);
            if (avatarState.agentAddress != context.Signer)
            {
                throw new InvalidAddressException();
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
            var requiredPotion = explorer.Floor * UnitApPotion;
            var sheets = states.GetSheets(
                containSimulatorSheets: true,
                sheetTypes: new[]
                {
                    typeof(ItemRequirementSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                    typeof(MaterialItemSheet),
                    typeof(RuneListSheet),
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
                    $"{requiredPotion} AP potions needed. You only have {inventory.Items.First(item => item.item.ItemSubType == ItemSubType.ApStone).count}"
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
            var costumeIds = avatarState.ValidateCostumeV2(Costumes, gameConfigState);
            var items = Equipments.Concat(Costumes);
            avatarState.EquipItems(items);
            avatarState.ValidateItemRequirement(
                costumeIds,
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

            exploreBoard.AddExplorer(AvatarAddress, avatarState.name);
            exploreBoard.UsedApPotion += requiredPotion;
            explorer.UsedApPotion += requiredPotion;

            var simulator = new AdventureBossSimulator(
                latestSeason.BossId, explorer.Floor, context.GetRandom(),
                avatarState, sheets.GetSimulatorSheets(), logEvent: false
            );
            simulator.AddBreakthrough(1, explorer.Floor,
                sheets.GetSheet<AdventureBossFloorWaveSheet>());

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
                var pointRow = floorPointSheet[fl];
                point += random.Next(pointRow.MinPoint, pointRow.MaxPoint + 1);

                selector.Clear();
                var floorReward = floorSheet.Values.Where(row => row.AdventureBossId == bossId)
                    .First(row => row.Floor == fl);
                foreach (var reward in floorReward.Rewards)
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

            return states
                .SetInventory(AvatarAddress, inventory)
                .SetExploreBoard(Season, exploreBoard)
                .SetExplorer(Season, explorer);
        }
    }
}
