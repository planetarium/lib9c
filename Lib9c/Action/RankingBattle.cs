using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using MessagePack;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("ranking_battle8")]
    [MessagePackObject]
    public class RankingBattle : GameAction
    {
        public const int StageId = 999999;
        public static readonly BigInteger EntranceFee = 100;
        [Key(1)]
#pragma warning disable MsgPack003
        public Address avatarAddress;
#pragma warning restore MsgPack003
        [Key(2)]
        public Address enemyAddress;
        [Key(3)]
        public Address weeklyArenaAddress;
        [Key(4)]
        public List<Guid> costumeIds;
        [Key(5)]
        public List<Guid> equipmentIds;
        [Key(6)]
        public List<Guid> consumableIds;
        [Key(7)]
        public Dictionary? EnemyAvatarState;
        [Key(8)]
        public Dictionary? ArenaInfo;
        [Key(9)]
        public Dictionary? EnemyArenaInfo;

        public RankingBattle()
        {
        }

        [SerializationConstructor]
        public RankingBattle(
            Guid guid,
            Address avatarAddress,
            Address enemyAddress,
            Address weeklyArenaAddress,
            List<Guid> costumeIds,
            List<Guid> equipmentIds,
            List<Guid> consumableIds,
            Dictionary? enemyAvatarState,
            Dictionary? arenaInfo,
            Dictionary? enemyArenaInfo
        ) : base(guid)
        {
            this.avatarAddress = avatarAddress;
            this.enemyAddress = enemyAddress;
            this.weeklyArenaAddress = weeklyArenaAddress;
            this.costumeIds = costumeIds;
            this.equipmentIds = equipmentIds;
            this.consumableIds = consumableIds;
            EnemyAvatarState = enemyAvatarState;
            ArenaInfo = arenaInfo;
            EnemyArenaInfo = enemyArenaInfo;
        }

        // FIXME Delete Result field.
        [IgnoreMember]
        public BattleLog Result { get; private set; }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(weeklyArenaAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged);
            }

            // Avoid InvalidBlockStateRootHashException
            if (ctx.BlockIndex == 680341 && Id.Equals(new Guid("df37dbd8-5703-4dff-918b-ad22ee4c34c6")))
            {
                return states;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress, enemyAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose(
                "{AddressesHex}RankingBattle exec started. costume: ({CostumeIds}), equipment: ({EquipmentIds})",
                addressesHex,
                string.Join(",", costumeIds),
                string.Join(",", equipmentIds)
            );

            if (avatarAddress.Equals(enemyAddress))
            {
                throw new InvalidAddressException($"{addressesHex}Aborted as the signer tried to battle for themselves.");
            }

            if (!states.TryGetAvatarStateV2(ctx.Signer, avatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var items = equipmentIds.Concat(costumeIds);

            avatarState.ValidateEquipmentsV2(equipmentIds, context.BlockIndex);
            avatarState.ValidateConsumable(consumableIds, context.BlockIndex);
            avatarState.ValidateCostume(costumeIds);

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Validate Equipments: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            avatarState.EquipItems(items);

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Equip Equipments: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(out var world) ||
                world.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)
            {
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                    world.StageClearedId);
            }

            AvatarState enemyAvatarState;
            try
            {
                enemyAvatarState = states.GetAvatarStateV2(enemyAddress);
            }
            // BackWard compatible.
            catch (FailedLoadStateException)
            {
                enemyAvatarState = states.GetAvatarState(enemyAddress);
            }
            if (enemyAvatarState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the opponent ({enemyAddress}) was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Get Enemy AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var weeklyArenaState = states.GetWeeklyArenaState(weeklyArenaAddress);

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Get WeeklyArenaState ({Address}): {Elapsed}", addressesHex, weeklyArenaAddress, sw.Elapsed);
            sw.Restart();

            if (weeklyArenaState.Ended)
            {
                throw new WeeklyArenaStateAlreadyEndedException();
            }

            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Get CostumeStatSheet: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!weeklyArenaState.ContainsKey(avatarAddress))
            {
                var characterSheet = states.GetSheet<CharacterSheet>();
                weeklyArenaState.SetV2(avatarState, characterSheet, costumeStatSheet);
                sw.Stop();
                Log.Verbose("{AddressesHex}RankingBattle Set AvatarInfo: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();
            }

            var arenaInfo = weeklyArenaState[avatarAddress];

            if (arenaInfo.DailyChallengeCount <= 0)
            {
                throw new NotEnoughWeeklyArenaChallengeCountException(
                    addressesHex + NotEnoughWeeklyArenaChallengeCountException.BaseMessage);
            }

            if (!arenaInfo.Active)
            {
                arenaInfo.Activate();
            }

            if (!weeklyArenaState.ContainsKey(enemyAddress))
            {
                throw new WeeklyArenaStateNotContainsAvatarAddressException(addressesHex, enemyAddress);
            }

            var enemyArenaInfo = weeklyArenaState[enemyAddress];
            if (!enemyArenaInfo.Active)
            {
                enemyArenaInfo.Activate();
            }

            Log.Verbose("{WeeklyArenaStateAddress}", weeklyArenaState.address.ToHex());

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Validate ArenaInfo: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var serializedArenaInfo = (Dictionary)arenaInfo.Serialize();
            var serializedEnemyArenaInfo = (Dictionary)enemyArenaInfo.Serialize();
            var serializedEnemyAvatarState = (Dictionary)enemyAvatarState.Serialize();
            var simulator = new RankingSimulator(
                ctx.Random,
                avatarState,
                enemyAvatarState,
                consumableIds,
                states.GetRankingSimulatorSheets(),
                StageId,
                arenaInfo,
                enemyArenaInfo,
                costumeStatSheet);

            simulator.Simulate();

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

            Result = simulator.Log;

            foreach (var itemBase in simulator.Reward.OrderBy(i => i.Id))
            {
                Log.Verbose(
                    "{AddressesHex}RankingBattle Add Reward Item({ItemBaseId}): {Elapsed}",
                    addressesHex,
                    itemBase.Id,
                    sw.Elapsed);
                avatarState.inventory.AddItem(itemBase);
            }

            states = states.SetState(weeklyArenaAddress, weeklyArenaState.Serialize());

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Serialize WeeklyArenaState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());

            sw.Stop();
            Log.Verbose("{AddressesHex}RankingBattle Serialize AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}RankingBattle Total Executed Time: {Elapsed}", addressesHex, ended - started);
            EnemyAvatarState = serializedEnemyAvatarState;
            EnemyArenaInfo = serializedEnemyArenaInfo;
            ArenaInfo = serializedArenaInfo;
            return states;
        }

        [Key(8)]
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
                ["consumable_ids"] = new List(consumableIds
                    .OrderBy(element => element)
                    .Select(e => e.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            enemyAddress = plainValue["enemyAddress"].ToAddress();
            weeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
            costumeIds = ((List) plainValue["costume_ids"])
                .Select(e => e.ToGuid())
                .ToList();
            equipmentIds = ((List) plainValue["equipment_ids"])
                .Select(e => e.ToGuid())
                .ToList();
            consumableIds = ((List) plainValue["consumable_ids"])
                .Select(e => e.ToGuid())
                .ToList();
        }
    }
}
