using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Action;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Model;
using Nekoyume.Model.Exceptions;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/941
    /// Updated at https://github.com/planetarium/lib9c/pull/1135
    /// </summary>
    [Serializable]
    [ActionType("ranking_battle12")]
    public class RankingBattle : GameAction, IRankingBattleV2
    {
        public const int StageId = 999999;

        public Address avatarAddress;
        public Address enemyAddress;
        public Address weeklyArenaAddress;
        public List<Guid> costumeIds;
        public List<Guid> equipmentIds;
        public EnemyPlayerDigest PreviousEnemyPlayerDigest;
        public ArenaInfo PreviousArenaInfo;
        public ArenaInfo PreviousEnemyArenaInfo;

        Address IRankingBattleV2.AvatarAddress => avatarAddress;
        Address IRankingBattleV2.EnemyAddress => enemyAddress;
        Address IRankingBattleV2.WeeklyArenaAddress => weeklyArenaAddress;
        IEnumerable<Guid> IRankingBattleV2.CostumeIds => costumeIds;
        IEnumerable<Guid> IRankingBattleV2.EquipmentIds => equipmentIds;

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var ctx = context;
            var world = ctx.PreviousState;
            if (ctx.Rehearsal)
            {
                world = AvatarModule.MarkChanged(world, avatarAddress, true, true, true, true);
                world = LegacyModule.SetState(world, weeklyArenaAddress, MarkChanged);
                return world;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress, enemyAddress);

            var arenaSheetAddress = Addresses.GetSheetAddress<ArenaSheet>();
            var arenaSheetState = LegacyModule.GetState(world, arenaSheetAddress);
            if (arenaSheetState != null)
            {
                // exception handling for v100240.
                if (context.BlockIndex > 4374126 && context.BlockIndex < 4374162)
                {
                }
                else
                {
                    throw new ActionObsoletedException(nameof(RankingBattle));
                }
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug(
                "{AddressesHex}RankingBattle exec started. costume: ({CostumeIds}), equipment: ({EquipmentIds})",
                addressesHex,
                string.Join(",", costumeIds),
                string.Join(",", equipmentIds)
            );

            if (avatarAddress.Equals(enemyAddress))
            {
                throw new InvalidAddressException(
                    $"{addressesHex}Aborted as the signer tried to battle for themselves.");
            }

            if (!AvatarModule.TryGetAvatarState(
                    world,
                    ctx.Signer,
                    avatarAddress,
                    out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            var sheets = LegacyModule.GetSheetsV100291(
                world,
                containRankingSimulatorSheets: true,
                sheetTypes: new[]
                {
                    typeof(CharacterSheet),
                    typeof(CostumeStatSheet),
                });
            sw.Stop();
            Log.Verbose("{AddressesHex}HAS Get Sheets: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var equipments = avatarState.ValidateEquipmentsV2(equipmentIds, context.BlockIndex);
            var costumeItemIds = avatarState.ValidateCostume(costumeIds);

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Validate Equipments: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var items = equipmentIds.Concat(costumeIds);
            avatarState.EquipItems(items);
            avatarState.ValidateItemRequirement(
                costumeItemIds.ToList(),
                equipments,
                LegacyModule.GetSheet<ItemRequirementSheet>(world),
                LegacyModule.GetSheet<EquipmentItemRecipeSheet>(world),
                LegacyModule.GetSheet<EquipmentItemSubRecipeSheetV2>(world),
                LegacyModule.GetSheet<EquipmentItemOptionSheet>(world),
                addressesHex);

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Equip Equipments: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(out var worldInfo) ||
                worldInfo.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)
            {
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                    worldInfo.StageClearedId);
            }

            AvatarState enemyAvatarState = AvatarModule.GetAvatarState(world, enemyAddress);

            if (enemyAvatarState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the opponent ({enemyAddress}) was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Get Enemy AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var costumeStatSheet = LegacyModule.GetSheet<CostumeStatSheet>(world);
            if (!LegacyModule.TryGetState(world, weeklyArenaAddress, out Dictionary rawWeeklyArenaState))
            {
                return world;
            }

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}RankingBattle Get WeeklyArenaState ({Address}): {Elapsed}",
                addressesHex,
                weeklyArenaAddress,
                sw.Elapsed);
            sw.Restart();

            var arenaEnded = rawWeeklyArenaState["ended"].ToBoolean();
            if (arenaEnded)
            {
                throw new WeeklyArenaStateAlreadyEndedException();
            }

            // Run updated model
            var (arenaInfoAddress, previousArenaInfo, isNewArenaInfo) = LegacyModule.GetArenaInfo(
                world,
                weeklyArenaAddress,
                avatarState,
                sheets.GetSheet<CharacterSheet>(),
                sheets.GetSheet<CostumeStatSheet>());
            PreviousArenaInfo = previousArenaInfo;
            var arenaInfo = PreviousArenaInfo.Clone();
            if (arenaInfo.DailyChallengeCount <= 0)
            {
                throw new NotEnoughWeeklyArenaChallengeCountException(
                    addressesHex + NotEnoughWeeklyArenaChallengeCountException.BaseMessage);
            }

            var rankingSheets = sheets.GetRankingSimulatorSheetsV100291();
            var player = new Player(avatarState, rankingSheets);
            PreviousEnemyPlayerDigest = new EnemyPlayerDigest(enemyAvatarState);
            var random = ctx.GetRandom();
            var simulator = new RankingSimulator(
                random,
                player,
                PreviousEnemyPlayerDigest,
                new List<Guid>(),
                rankingSheets,
                StageId,
                costumeStatSheet);
            simulator.Simulate();
            var (enemyArenaInfoAddress, previousEnemyArenaInfo, isNewEnemyArenaInfo) = LegacyModule.GetArenaInfo(
                world,
                weeklyArenaAddress,
                enemyAvatarState,
                sheets.GetSheet<CharacterSheet>(),
                sheets.GetSheet<CostumeStatSheet>());
            PreviousEnemyArenaInfo = previousEnemyArenaInfo;
            var enemyArenaInfo = PreviousEnemyArenaInfo.Clone();
            var challengerScoreDelta = arenaInfo.Update(
                enemyArenaInfo,
                simulator.Result,
                ArenaScoreHelper.GetScoreV4);
            var rewards = RewardSelector.Select(
                random,
                sheets.GetSheet<WeeklyArenaRewardSheet>(),
                sheets.GetSheet<MaterialItemSheet>(),
                player.Level,
                arenaInfo.GetRewardCount());
            simulator.PostSimulate(rewards, challengerScoreDelta, arenaInfo.Score);

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}RankingBattle Simulate() with equipment:({Equipment}), costume:({Costume}): {Elapsed}",
                addressesHex,
                string.Join(",", simulator.Player.Equipments.Select(r => r.ItemId)),
                string.Join(",", simulator.Player.Costumes.Select(r => r.ItemId)),
                sw.Elapsed
            );

            Log.Verbose(
                "{AddressesHex}Execute RankingBattle({AvatarAddress}); result: {Result} event count: {EventCount}",
                addressesHex,
                avatarAddress,
                simulator.Log.result,
                simulator.Log.Count
            );
            sw.Restart();

            foreach (var itemBase in simulator.Reward.OrderBy(i => i.Id))
            {
                Log.Verbose(
                    "{AddressesHex}RankingBattle Add Reward Item({ItemBaseId}): {Elapsed}",
                    addressesHex,
                    itemBase.Id,
                    sw.Elapsed);
                avatarState.inventory.AddItem(itemBase);
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Serialize WeeklyArenaState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            world = LegacyModule.SetState(world, arenaInfoAddress, arenaInfo.Serialize());
            world = LegacyModule.SetState(world, enemyArenaInfoAddress, enemyArenaInfo.Serialize());

            world = AvatarModule.SetAvatarState(world, avatarAddress, avatarState, false, true, false, true);

            if (isNewArenaInfo || isNewEnemyArenaInfo)
            {
                var addressListAddress = weeklyArenaAddress.Derive("address_list");
                var addressList = LegacyModule.TryGetState(
                    world,
                    addressListAddress,
                    out List rawAddressList)
                    ? rawAddressList.ToList(StateExtensions.ToAddress)
                    : new List<Address>();

                if (!addressList.Contains(avatarAddress))
                {
                    addressList.Add(avatarAddress);
                }

                if (!addressList.Contains(enemyAddress))
                {
                    addressList.Add(enemyAddress);
                }

                world = LegacyModule.SetState(
                    world,
                    addressListAddress,
                    addressList.Aggregate(
                        List.Empty,
                        (current, address) => current.Add(address.Serialize())));
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Serialize AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RankingBattle Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return world;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["enemyAddress"] = enemyAddress.Serialize(),
                ["weeklyArenaAddress"] = weeklyArenaAddress.Serialize(),
                ["costume_ids"] = new List(costumeIds
                    .OrderBy(element => element)
                    .Select(e => e.Serialize())),
                ["equipment_ids"] = new List(equipmentIds
                    .OrderBy(element => element)
                    .Select(e => e.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            enemyAddress = plainValue["enemyAddress"].ToAddress();
            weeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
            costumeIds = ((List)plainValue["costume_ids"])
                .Select(e => e.ToGuid())
                .ToList();
            equipmentIds = ((List)plainValue["equipment_ids"])
                .Select(e => e.ToGuid())
                .ToList();
        }
    }
}
