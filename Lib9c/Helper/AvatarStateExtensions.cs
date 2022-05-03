using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    public static class AvatarStateExtensions
    {
        public static void UpdateMonsterMap(this AvatarState avatarState,
            StageWaveSheet stageWaveSheet, int stageId)
        {
            var monsterMap = new CollectionMap();
            if (stageWaveSheet.TryGetValue(stageId, out var stageWaveRow))
            {
                foreach (var monster in stageWaveRow.Waves.SelectMany(wave => wave.Monsters))
                {
                    monsterMap.Add(new KeyValuePair<int, int>(monster.CharacterId, monster.Count));
                }
            }

            avatarState.questList.UpdateMonsterQuest(monsterMap);
        }

        public static void UpdateInventory(this AvatarState avatarState, List<ItemBase> rewards)
        {
            var itemMap = new CollectionMap();
            foreach (var reward in rewards)
            {
                itemMap.Add(avatarState.inventory.AddItem(reward));
            }

            avatarState.questList.UpdateCollectQuest(itemMap);
        }

        public static void UpdateExp(this AvatarState avatarState, int level, long exp)
        {
            var levelUpCount = level - avatarState.level;
            var eventMap = new CollectionMap
                { new KeyValuePair<int, int>((int)QuestEventType.Level, levelUpCount) };
            avatarState.level = level;
            avatarState.exp = exp;
            avatarState.questList.UpdateCompletedQuest(eventMap);
        }

        public static (int, long) GetLevelAndExp(this AvatarState avatarState,
            CharacterLevelSheet characterLevelSheet, int stageId, int repeatCount)
        {
            var remainCount = repeatCount;
            var currentLevel = avatarState.level;
            var currentExp = avatarState.exp;
            while (remainCount > 0)
            {
                characterLevelSheet.TryGetValue(currentLevel, out var row, true);
                var maxExp = row.Exp + row.ExpNeed;
                var remainExp = maxExp - currentExp;
                var stageExp = StageRewardExpHelper.GetExp(currentLevel, stageId);
                var requiredCount = (int)Math.Ceiling(remainExp / (double)stageExp);
                if (remainCount - requiredCount > 0) // level up
                {
                    currentExp += stageExp * requiredCount;
                    remainCount -= requiredCount;
                    currentLevel += 1;
                }
                else
                {
                    currentExp += stageExp * remainCount;
                    break;
                }
            }

            return (currentLevel, currentExp);
        }
    }
}
