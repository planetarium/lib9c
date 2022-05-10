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
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("arena_battle")]
    public class ArenaBattle : GameAction
    {
        public Address avatarAddress;
        public Address enemyAddress;
        public List<Guid> costumeIds;
        public List<Guid> equipmentIds;
        public int ticket;
        public int arenaId;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["enemyAddress"] = enemyAddress.Serialize(),
                ["costume_ids"] = new List(costumeIds
                    .OrderBy(element => element).Select(e => e.Serialize())),
                ["equipment_ids"] = new List(equipmentIds
                    .OrderBy(element => element).Select(e => e.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            enemyAddress = plainValue["enemyAddress"].ToAddress();
            costumeIds = ((List)plainValue["costume_ids"]).Select(e => e.ToGuid()).ToList();
            equipmentIds = ((List)plainValue["equipment_ids"]).Select(e => e.ToGuid()).ToList();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var blockIndex = context.BlockIndex;
            var arenaAvatarStateAddress = ArenaAvatarState.DeriveAddress(avatarAddress);
            var arenaStateAddress = ArenaState.DeriveAddress(arenaId);
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);

            if (context.Rehearsal)
            {
                return states
                    .SetState(arenaStateAddress, MarkChanged)
                    .SetState(arenaAvatarStateAddress, MarkChanged)
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress, enemyAddress);

            if (avatarAddress.Equals(enemyAddress))
            {
                throw new InvalidAddressException(
                    $"{addressesHex}Aborted as the signer tried to battle for themselves.");
            }

            if (!states.TryGetAvatarStateV2(context.Signer, avatarAddress, out var avatarState,
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
                containRankingSimulatorSheets: true,
                sheetTypes: new[]
                {
                    typeof(CharacterSheet),
                    typeof(CostumeStatSheet),
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
                blockIndex, addressesHex);

            if (!states.TryGetState(arenaAvatarStateAddress, out Dictionary arenaAvatarStateDic))
            {
                // 아레나는 아레나 참가 액션에서 만들어준다.
                // todo :에러 처리
            }

            // var arenaAvatarState = new ArenaAvatarState(arenaAvatarStateDic);
            // arenaAvatarState.UpdateEquipment(equipmentIds);
            // arenaAvatarState.UpdateCostumes(costumeIds);
            // if (!arenaAvatarState.TryUseTicket(ticket))
            // {
            //     // todo : 에러 처리
            // }
            //
            // var score = 999;
            // // arenaAvatarState.Records.Update();
            //
            // if (!states.TryGetState(arenaStateAddress, out List arenaStateDic))
            // {
            //     // 아레나기록은 아레나 참가 액션에서 만들어준다.
            //     // todo :에러 처리
            // }
            //
            // var arenaState = new ArenaState(arenaStateDic);



            // var simulator = new RankingSimulator(
            //     context.Random,
            //     player,
            //     PreviousEnemyPlayerDigest,
            //     new List<Guid>(),
            //     rankingSheets,
            //     StageId,
            //     costumeStatSheet);
            // simulator.Simulate();

            return states
                // .SetState(arenaStateAddress, arenaState.SerializeV2())
                // .SetState(arenaAvatarStateAddress, arenaAvatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());
        }
    }
}
