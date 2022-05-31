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
    [ActionObsolete(BlockChain.Policy.BlockPolicySource.V100200ObsoleteIndex)]
    [ActionType("hack_and_slash_sweep2")]
    public class HackAndSlashSweep2 : GameAction
    {
        public const int UsableApStoneCount = 10;

        public Address avatarAddress;
        public int apStoneCount = 0;
        public int worldId;
        public int stageId;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["apStoneCount"] = apStoneCount.Serialize(),
                ["worldId"] = worldId.Serialize(),
                ["stageId"] = stageId.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            apStoneCount = plainValue["apStoneCount"].ToInteger();
            worldId = plainValue["worldId"].ToInteger();
            stageId = plainValue["stageId"].ToInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                return states
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(context.Signer, MarkChanged);
            }

            CheckObsolete(BlockChain.Policy.BlockPolicySource.V100200ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (apStoneCount > UsableApStoneCount)
            {
                throw new UsageLimitExceedException($"Exceeded the amount of ap stones that can be used " +
                                                    $"apStoneCount : {apStoneCount} > UsableApStoneCount : {UsableApStoneCount}");
            }

            if (worldId == GameConfig.MimisbrunnrWorldId)
            {
                throw new InvalidWorldException(
                    $"{addressesHex} [{worldId}] can't execute HackAndSlashSweep action.");
            }

            if (!states.TryGetAvatarStateV2(context.Signer, avatarAddress, out var avatarState, out var migrationRequired))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var sheets = states.GetSheets(
                containQuestSheet: false,
                containStageSimulatorSheets: false,
                sheetTypes: new[]
                {
                    typeof(WorldSheet),
                    typeof(StageSheet),
                    typeof(MaterialItemSheet),
                    typeof(StageWaveSheet),
                    typeof(CharacterLevelSheet),
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
            if (!worldInformation.TryGetWorld(worldId, out var world))
            {
                throw new SheetRowColumnException($"{addressesHex}world is not contains in world information: {worldId}");
            }

            if (!world.IsStageCleared)
            {
                throw new StageNotClearedException($"{addressesHex}There is no stage cleared in that world (worldId:{worldId})");
            }

            if (stageId > world.StageClearedId)
            {
                throw new InvalidStageException(
                    $"{addressesHex}Aborted as the stage ({worldId}/{stageId}) is not cleared; " +
                    $"cleared stage: {world.StageClearedId}"
                );
            }

            var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
            if (apStoneCount > 0)
            {
                // use apStone
                var row = materialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
                if (!avatarState.inventory.RemoveFungibleItem(row.ItemId, context.BlockIndex, count: apStoneCount))
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex}Aborted as the player has no enough material ({row.Id})");
                }
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the game config state was failed to load.");
            }

            var apStonePlayCount = gameConfigState.ActionPointMax / stageRow.CostAP * apStoneCount;
            var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
            var playCount = apStonePlayCount + apPlayCount;
            if (playCount <= 0)
            {
                var ap = avatarState.actionPoint + gameConfigState.ActionPointMax * apStoneCount;
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: {ap} < required cost : {stageRow.CostAP})"
                );
            }

            // burn ap
            var remainActionPoint = Math.Max(0, avatarState.actionPoint - stageRow.CostAP * apPlayCount);
            avatarState.actionPoint = remainActionPoint;

            var stageWaveSheet = sheets.GetSheet<StageWaveSheet>();
            avatarState.UpdateMonsterMap(stageWaveSheet, stageId);

            var rewardItems = GetRewardItems(context.Random, playCount, stageRow, materialItemSheet);
            avatarState.UpdateInventory(rewardItems);

            var levelSheet = sheets.GetSheet<CharacterLevelSheet>();
            var (level, exp) = avatarState.GetLevelAndExpV1(levelSheet, stageId, playCount);
            avatarState.UpdateExp(level, exp);

            return states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());
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
                var selector = StageSimulator.SetItemSelector(stageRow, random);
                var rewards = Simulator.SetRewardV2(selector, maxCount, random,
                    materialItemSheet);
                rewardItems.AddRange(rewards);
            }

            rewardItems = rewardItems.OrderBy(x => x.Id).ToList();
            return rewardItems;
        }
    }
}
