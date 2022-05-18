using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("battle_arena")]
    public class BattleArena : GameAction
    {
        public Address myAvatarAddress;
        public Address enemyAvatarAddress;
        public List<Guid> costumeIds;
        public List<Guid> equipmentIds;
        public int ticket;
        public int championshipId;
        public int round;

        public List<BattleLog.Result> Results = new List<BattleLog.Result>();

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                ["myAvatarAddress"] = myAvatarAddress.Serialize(),
                ["enemyAvatarAddress"] = enemyAvatarAddress.Serialize(),
                ["championshipId"] = championshipId.Serialize(),
                ["round"] = round.Serialize(),
                ["costume_ids"] = new List(costumeIds
                    .OrderBy(element => element).Select(e => e.Serialize())),
                ["equipment_ids"] = new List(equipmentIds
                    .OrderBy(element => element).Select(e => e.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            myAvatarAddress = plainValue["myAvatarAddress"].ToAddress();
            enemyAvatarAddress = plainValue["enemyAvatarAddress"].ToAddress();
            championshipId = plainValue["championshipId"].ToInteger();
            round = plainValue["round"].ToInteger();
            costumeIds = ((List)plainValue["costume_ids"]).Select(e => e.ToGuid()).ToList();
            equipmentIds = ((List)plainValue["equipment_ids"]).Select(e => e.ToGuid()).ToList();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var addressesHex =
                GetSignerAndOtherAddressesHex(context, myAvatarAddress, enemyAvatarAddress);

            if (myAvatarAddress.Equals(enemyAvatarAddress))
            {
                throw new InvalidAddressException(
                    $"{addressesHex}Aborted as the signer tried to battle for themselves.");
            }

            if (!states.TryGetAvatarStateV2(context.Signer, myAvatarAddress, out var avatarState,
                    out var _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(
                    out var world))
            {
                throw new NotEnoughClearedStageLevelException(
                    $"{addressesHex}Aborted as NotEnoughClearedStageLevelException");
            }

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)
            {
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                    world.StageClearedId);
            }

            var sheets = states.GetSheets(
                containArenaSimulatorSheets: true,
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                    typeof(ItemRequirementSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                });

            avatarState.ValidEquipmentAndCostume(costumeIds, equipmentIds,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                context.BlockIndex, addressesHex);

            var enemyAvatarState = GetEnemyAvatarState(states, enemyAvatarAddress);

            var inventoryAddress = myAvatarAddress.Derive(LegacyInventoryKey);
            var questListAddress = myAvatarAddress.Derive(LegacyQuestListKey);

            var sheet = sheets.GetSheet<ArenaSheet>();
            if (!sheet.TryGetValue(championshipId, out var row))
            {
                throw new SheetRowNotFoundException(
                    nameof(ArenaSheet), $"championship Id : {championshipId}");
            }

            if (!row.IsTheRoundOpened(context.BlockIndex, championshipId, round))
            {
                // todo: 시트값 + 현재 블록인덱스 체크해서 진행중인 아레나인지 체크
            }

            var arenaParticipantsAdr = ArenaParticipants.DeriveAddress(championshipId, round);
            if (!states.TryGetState(arenaParticipantsAdr, out List arenaParticipantsList))
            {
                // todo : 참가자가 없네?
            }

            var arenaParticipants = new ArenaParticipants(arenaParticipantsList);

            if (!arenaParticipants.AvatarAddresses.Contains(myAvatarAddress))
            {
                // todo : 나 참가안함
            }

            if (!arenaParticipants.AvatarAddresses.Contains(enemyAvatarAddress))
            {
                // todo : 적 참가안함
            }

            // get my arena avatar state
            var myArenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(myAvatarAddress);
            if (states.TryGetState(myArenaAvatarStateAdr, out List myArenaAvatarStateList))
            {
                // todo : 나으 아레나 아바타 상태없음
            }

            var myArenaAvatarState = new ArenaAvatarState(myArenaAvatarStateList);
            myArenaAvatarState.UpdateEquipment(equipmentIds);
            myArenaAvatarState.UpdateCostumes(costumeIds);

            // get enemy arena avatar state
            var enemyArenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(enemyAvatarAddress);
            if (states.TryGetState(enemyArenaAvatarStateAdr, out List enemyArenaAvatarStateList))
            {
                // todo : 적 아레나 아바타 상태없음
            }

            var enemyArenaAvatarState = new ArenaAvatarState(enemyArenaAvatarStateList);


            // get my arena score
            var myArenaScoreAdr = ArenaScore.DeriveAddress(myAvatarAddress, championshipId, round);
            if (!states.TryGetState(myArenaScoreAdr, out List myArenaScoreList))
            {
                // todo : 나으 아레나 아바타 상태없음
            }

            var myArenaScore = new ArenaScore(myArenaScoreList);

            // // get enemy arena score
            var enemyArenaScoreAdr =
                ArenaScore.DeriveAddress(enemyAvatarAddress, championshipId, round);
            if (!states.TryGetState(enemyArenaScoreAdr, out List enemyArenaScoreList))
            {
                // todo : 나으 아레나 아바타 상태없음
            }

            var enemyArenaScore = new ArenaScore(enemyArenaScoreList);

            // get my arena information
            var arenaInformationAdr =
                ArenaInformation.DeriveAddress(enemyAvatarAddress, championshipId, round);
            if (!states.TryGetState(arenaInformationAdr, out List arenaInformationList))
            {
                // todo : 내정보 없음
            }

            // use ticket
            var arenaInformation = new ArenaInformation(arenaInformationList);
            arenaInformation.UseTicket(ticket); // 티켓은 한번에 빼줌?

            // simulate
            var myDigest = new ArenaPlayerDigest(avatarState, myArenaAvatarState);
            var enemyDigest = new ArenaPlayerDigest(enemyAvatarState, enemyArenaAvatarState);
            var arenaSheets = sheets.GetArenaSimulatorSheets();

            for (var i = 0; i < ticket; i++)
            {
                var simulator =
                    new ArenaSimulator(context.Random, myDigest, enemyDigest, arenaSheets);
                simulator.Simulate();
                var (myScore, enemyScore) = ArenaScoreHelper.GetScore(
                    myArenaScore.Score, enemyArenaScore.Score, simulator.Result);
                myArenaScore.AddScore(myScore);
                enemyArenaScore.AddScore(enemyScore);
                arenaInformation.UpdateRecord(simulator.Result);
                Results.Add(simulator.Result);

                // 바뀐 점수에따라 얻는 스코어가 다르다면 여기서 set state
                states = states
                    .SetState(myArenaScoreAdr, myArenaScore.Serialize())
                    .SetState(enemyArenaScoreAdr, enemyArenaScore.Serialize());
            }

            // todo: 보상 아이템(메달) 처리 해줘야함

            return states
                .SetState(myArenaAvatarStateAdr, myArenaAvatarState.Serialize())
                .SetState(enemyArenaAvatarStateAdr, enemyArenaAvatarState.Serialize())
                .SetState(arenaInformationAdr, arenaInformation.Serialize())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(myAvatarAddress, avatarState.SerializeV2());
        }

        private AvatarState GetEnemyAvatarState(IAccountStateDelta states, Address avatarAddress)
        {
            AvatarState enemyAvatarState;
            try
            {
                enemyAvatarState = states.GetAvatarStateV2(avatarAddress);
            }
            // BackWard compatible.
            catch (FailedLoadStateException)
            {
                enemyAvatarState = states.GetAvatarState(avatarAddress);
            }

            if (enemyAvatarState is null)
            {
                throw new FailedLoadStateException(
                    $"Aborted as the avatar state of the opponent ({avatarAddress}) was failed to load.");
            }

            return enemyAvatarState;
        }
    }
}
