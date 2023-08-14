using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1663
    /// </summary>
    [Serializable]
    [ActionType("hack_and_slash_sweep9")]
    public class HackAndSlashSweep : GameAction, IHackAndSlashSweepV3
    {
        public const int UsableApStoneCount = 10;

        public List<Guid> costumes;
        public List<Guid> equipments;
        public List<RuneSlotInfo> runeInfos;
        public Address avatarAddress;
        public int apStoneCount;
        public int actionPoint;
        public int worldId;
        public int stageId;

        IEnumerable<Guid> IHackAndSlashSweepV3.Costumes => costumes;
        IEnumerable<Guid> IHackAndSlashSweepV3.Equipments => equipments;
        IEnumerable<IValue> IHackAndSlashSweepV3.RuneSlotInfos =>
            runeInfos.Select(x => x.Serialize());
        Address IHackAndSlashSweepV3.AvatarAddress => avatarAddress;
        int IHackAndSlashSweepV3.ApStoneCount => apStoneCount;
        int IHackAndSlashSweepV3.ActionPoint => actionPoint;
        int IHackAndSlashSweepV3.WorldId => worldId;
        int IHackAndSlashSweepV3.StageId => stageId;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                ["costumes"] = new List(costumes.OrderBy(i => i).Select(e => e.Serialize())),
                ["equipments"] = new List(equipments.OrderBy(i => i).Select(e => e.Serialize())),
                ["runeInfos"] = runeInfos.OrderBy(x => x.SlotIndex).Select(x=> x.Serialize()).Serialize(),
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["apStoneCount"] = apStoneCount.Serialize(),
                ["actionPoint"] = actionPoint.Serialize(),
                ["worldId"] = worldId.Serialize(),
                ["stageId"] = stageId.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            runeInfos = plainValue["runeInfos"].ToList(x => new RuneSlotInfo((List)x));
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            apStoneCount = plainValue["apStoneCount"].ToInteger();
            actionPoint = plainValue["actionPoint"].ToInteger();
            worldId = plainValue["worldId"].ToInteger();
            stageId = plainValue["stageId"].ToInteger();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            if (context.Rehearsal)
            {
                return context.PreviousState;
            }

            var world = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}HackAndSlashSweep exec started", addressesHex);

            if (apStoneCount > UsableApStoneCount)
            {
                throw new UsageLimitExceedException(
                    $"Exceeded the amount of ap stones that can be used " +
                    $"apStoneCount : {apStoneCount} > UsableApStoneCount : {UsableApStoneCount}");
            }

            LegacyModule.ValidateWorldId(world, avatarAddress, worldId);

            if (!AvatarModule.TryGetAvatarStateV2(
                    world,
                    context.Signer,
                    avatarAddress,
                    out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var sheets = LegacyModule.GetSheets(
                world,
                sheetTypes: new[]
                {
                    typeof(WorldSheet),
                    typeof(StageSheet),
                    typeof(MaterialItemSheet),
                    typeof(StageWaveSheet),
                    typeof(CharacterLevelSheet),
                    typeof(ItemRequirementSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                    typeof(CharacterSheet),
                    typeof(CostumeStatSheet),
                    typeof(SweepRequiredCPSheet),
                    typeof(StakeActionPointCoefficientSheet),
                    typeof(RuneListSheet),
                    typeof(RuneOptionSheet),
                });

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

            if (!sheets.GetSheet<StageSheet>().TryGetValue(stageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(StageSheet), stageId);
            }

            var worldInformation = avatarState.worldInformation;
            if (!worldInformation.TryGetWorld(worldId, out var worldInfo))
            {
                // NOTE: Add new World from WorldSheet
                worldInformation.AddAndUnlockNewWorld(worldRow, context.BlockIndex, worldSheet);
                if (!worldInformation.TryGetWorld(worldId, out worldInfo))
                {
                    // Do nothing.
                }
            }

            if (!worldInfo.IsPlayable(stageId))
            {
                throw new InvalidStageException(
                    $"{addressesHex}Aborted as the stage isn't playable;" +
                    $"StageClearedId: {worldInfo.StageClearedId}"
                );
            }

            var equipmentList = avatarState.ValidateEquipmentsV2(equipments, context.BlockIndex);
            var costumeIds = avatarState.ValidateCostume(costumes);
            var items = equipments.Concat(costumes);
            avatarState.EquipItems(items);
            avatarState.ValidateItemRequirement(
                costumeIds,
                equipmentList,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                addressesHex);

            var sweepRequiredCpSheet = sheets.GetSheet<SweepRequiredCPSheet>();
            if (!sweepRequiredCpSheet.TryGetValue(stageId, out var cpRow))
            {
                throw new SheetRowColumnException(
                    $"{addressesHex}There is no row in SweepRequiredCPSheet: {stageId}");
            }

            var costumeList = new List<Costume>();
            foreach (var guid in costumes)
            {
                var costume = avatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid);
                if (costume != null)
                {
                    costumeList.Add(costume);
                }
            }

            // update rune slot
            var runeSlotStateAddress = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
            var runeSlotState = LegacyModule.TryGetState(
                world,
                runeSlotStateAddress,
                out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Adventure);
            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            runeSlotState.UpdateSlot(runeInfos, runeListSheet);
            world = LegacyModule.SetState(world, runeSlotStateAddress, runeSlotState.Serialize());

            // update item slot
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
            var itemSlotState = LegacyModule.TryGetState(
                world,
                itemSlotStateAddress,
                out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Adventure);
            itemSlotState.UpdateEquipment(equipments);
            itemSlotState.UpdateCostumes(costumes);
            world = LegacyModule.SetState(world, itemSlotStateAddress, itemSlotState.Serialize());

            var runeStates = new List<RuneState>();
            foreach (var address in runeInfos.Select(info => RuneState.DeriveAddress(avatarAddress, info.RuneId)))
            {
                if (LegacyModule.TryGetState(world, address, out List rawRuneState))
                {
                    runeStates.Add(new RuneState(rawRuneState));
                }
            }
            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
            var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            foreach (var runeState in runeStates)
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

            var characterSheet = sheets.GetSheet<CharacterSheet>();
            if (!characterSheet.TryGetValue(avatarState.characterId, out var characterRow))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            var cp = CPHelper.TotalCP(
                equipmentList, costumeList,
                runeOptions, avatarState.level,
                characterRow, costumeStatSheet);
            if (cp < cpRow.RequiredCP)
            {
                throw new NotEnoughCombatPointException(
                    $"{addressesHex}Aborted due to lack of player cp ({cp} < {cpRow.RequiredCP})");
            }

            var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
            if (apStoneCount > 0)
            {
                // use apStone
                var row = materialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
                if (!avatarState.inventory.RemoveFungibleItem(row.ItemId, context.BlockIndex,
                        count: apStoneCount))
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex}Aborted as the player has no enough material ({row.Id})");
                }
            }

            var gameConfigState = LegacyModule.GetGameConfigState(world);
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the game config state was failed to load.");
            }

            if (actionPoint > avatarState.actionPoint)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: " +
                    $"use AP({actionPoint}) > current AP({avatarState.actionPoint})"
                );
            }

            // burn ap
            avatarState.actionPoint -= actionPoint;
            var costAp = sheets.GetSheet<StageSheet>()[stageId].CostAP;
            if (LegacyModule.TryGetStakeState(world, context.Signer, out var stakeState))
            {
                var currency = LegacyModule.GetGoldCurrency(world);
                var stakedAmount = LegacyModule.GetBalance(world, stakeState.address, currency);
                var actionPointCoefficientSheet =
                    sheets.GetSheet<StakeActionPointCoefficientSheet>();
                var stakingLevel =
                    actionPointCoefficientSheet.FindLevelByStakedAmount(context.Signer,
                        stakedAmount);
                costAp = actionPointCoefficientSheet.GetActionPointByStaking(
                    costAp,
                    1,
                    stakingLevel);
            }

            var apMaxPlayCount = costAp > 0 ? gameConfigState.ActionPointMax / costAp : 0;
            var apStonePlayCount = apMaxPlayCount * apStoneCount;
            var apPlayCount = costAp > 0 ? actionPoint / costAp : 0;
            var playCount = apStonePlayCount + apPlayCount;
            if (playCount <= 0)
            {
                throw new PlayCountIsZeroException(
                    $"{addressesHex}playCount must be greater than 0. " +
                    $"current playCount : {playCount}");
            }

            var stageWaveSheet = sheets.GetSheet<StageWaveSheet>();
            avatarState.UpdateMonsterMap(stageWaveSheet, stageId);

            var rewardItems = GetRewardItems(
                context.Random,
                playCount,
                stageRow,
                materialItemSheet);
            avatarState.UpdateInventory(rewardItems);

            var levelSheet = sheets.GetSheet<CharacterLevelSheet>();
            var (level, exp) = avatarState.GetLevelAndExp(levelSheet, stageId, playCount);
            avatarState.UpdateExp(level, exp);

            if (migrationRequired)
            {
                world = LegacyModule.SetState(
                    world,
                    avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize());
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}HackAndSlashSweep Total Executed Time: {Elapsed}", addressesHex, ended - started);
            world = AvatarModule.SetAvatarStateV2(world, avatarAddress, avatarState);
            world = LegacyModule.SetState(
                world,
                avatarAddress.Derive(LegacyInventoryKey),
                avatarState.inventory.Serialize());
            world = LegacyModule.SetState(
                world,
                avatarAddress.Derive(LegacyQuestListKey),
                avatarState.questList.Serialize());
            return world;
        }

        public static List<ItemBase> GetRewardItems(IRandom random,
            int playCount,
            StageSheet.Row stageRow,
            MaterialItemSheet materialItemSheet)
        {
            var rewardItems = new List<ItemBase>();
            var maxCount = random.Next(stageRow.DropItemMin, stageRow.DropItemMax + 1);
            for (var i = 0; i < playCount; i++)
            {
                var selector = StageSimulatorV1.SetItemSelector(stageRow, random);
                var rewards = Simulator.SetRewardV2(selector, maxCount, random,
                    materialItemSheet);
                rewardItems.AddRange(rewards);
            }

            rewardItems = rewardItems.OrderBy(x => x.Id).ToList();
            return rewardItems;
        }
    }
}
