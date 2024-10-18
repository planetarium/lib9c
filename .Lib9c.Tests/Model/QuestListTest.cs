namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Quest;
    using Nekoyume.TableData;
    using Xunit;

    public class QuestListTest
    {
        private readonly TableSheets _tableSheets;

        public QuestListTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void SerializeExceptions()
        {
            ExceptionTest.AssertException(
                new UpdateListVersionException("test"),
                new UpdateListQuestsCountException("test"));
        }

        [Fact]
        public void GetEnumerator()
        {
            var list = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            ).ToList();

            for (var i = 0; i < list.Count - 1; i++)
            {
                var quest = list[i];
                var next = list[i + 1];
                Assert.True(quest.Id < next.Id);
            }
        }

        [Fact]
        public void UpdateItemTypeCollectQuestDeterministic()
        {
            var expectedItemIds = new List<int>
            {
                303000,
                303100,
                303200,
                306023,
                306040,
            };

            var itemIds = new List<int>
            {
                400000,
            };
            itemIds.AddRange(expectedItemIds);

            var prevItems = itemIds
                .Select(id => ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet[id]))
                .Cast<ItemBase>()
                .OrderByDescending(i => i.Id)
                .ToList();

            Assert.Equal(6, prevItems.Count);

            var list = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            );

            Assert.Empty(list.OfType<ItemTypeCollectQuest>().First().ItemIds);

            list.UpdateItemTypeCollectQuest(prevItems);

            Assert.Equal(expectedItemIds, list.OfType<ItemTypeCollectQuest>().First().ItemIds);
        }

        [Fact]
        public void UpdateList()
        {
            var questList = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            );

            Assert.Equal(1, questList.ListVersion);
            Assert.Equal(_tableSheets.QuestSheet.Count, questList.Count());

            var previousQuestCount = questList.Count();
            var questSheet = new QuestSheet();
            var patchedSheet = new WorldQuestSheet();
            var patchedSheetCsvSb = new StringBuilder().AppendLine("id,goal,quest_reward_id");
            var ids = new List<int>();
            var ceqCsv = @"id,goal,quest_reward_id,recipe_id
1100001,1,101,1";
            var ceqSheet = new CombinationEquipmentQuestSheet();
            ceqSheet.Set(ceqCsv);
            for (var i = 3; i > 0; i--)
            {
                var questId = 990000 + i - 1;
                ids.Add(questId);
                patchedSheetCsvSb.AppendLine($"{questId},10,{_tableSheets.QuestSheet.First!.QuestRewardId}");
            }

            patchedSheet.Set(patchedSheetCsvSb.ToString());
            questSheet.Set(patchedSheet, false);
            questSheet.Set(_tableSheets.CollectQuestSheet, false);
            questSheet.Set(_tableSheets.CombinationQuestSheet, false);
            questSheet.Set(_tableSheets.TradeQuestSheet, false);
            questSheet.Set(_tableSheets.MonsterQuestSheet, false);
            questSheet.Set(_tableSheets.ItemEnhancementQuestSheet, false);
            questSheet.Set(_tableSheets.GeneralQuestSheet, false);
            questSheet.Set(_tableSheets.ItemGradeQuestSheet, false);
            questSheet.Set(_tableSheets.ItemTypeCollectQuestSheet, false);
            questSheet.Set(_tableSheets.GoldQuestSheet, false);
            questSheet.Set(ceqSheet);
            Assert.True(previousQuestCount > questSheet.Count);

            questList.UpdateList(
                questSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                ids);
            Assert.Equal(1, questList.ListVersion);
            Assert.Equal(previousQuestCount + ids.Count, questList.Count());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(99)]
        public void UpdateListV1(int questCountToAdd)
        {
            var questList = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            );

            Assert.Equal(1, questList.ListVersion);
            Assert.Equal(_tableSheets.QuestSheet.Count, questList.Count());

            var questSheet = _tableSheets.QuestSheet;
            Assert.NotNull(questSheet.First);
            var patchedSheet = new WorldQuestSheet();
            var patchedSheetCsvSb = new StringBuilder().AppendLine("id,goal,quest_reward_id");
            for (var i = questCountToAdd; i > 0; i--)
            {
                patchedSheetCsvSb.AppendLine($"{990000 + i - 1},10,{questSheet.First.QuestRewardId}");
            }

            patchedSheet.Set(patchedSheetCsvSb.ToString());
            Assert.Equal(questCountToAdd, patchedSheet.Count);
            var previousQuestSheetCount = questSheet.Count;
            questSheet.Set(patchedSheet);
            Assert.Equal(previousQuestSheetCount + questCountToAdd, questSheet.Count);

            questList.UpdateListV1(
                2,
                questSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet);
            Assert.Equal(2, questList.ListVersion);
            Assert.Equal(questSheet.Count, questList.Count());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void UpdateList_Throw_UpdateListVersionException(int listVersion)
        {
            var questList = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            );

            Assert.Equal(1, questList.ListVersion);
            Assert.Throws<UpdateListVersionException>(() =>
                questList.UpdateListV1(
                    listVersion,
                    _tableSheets.QuestSheet,
                    _tableSheets.QuestRewardSheet,
                    _tableSheets.QuestItemRewardSheet,
                    _tableSheets.EquipmentItemRecipeSheet));
        }

        [Fact]
        public void UpdateList_Throw_UpdateListQuestsCountException()
        {
            var questList = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            );

            Assert.Equal(1, questList.ListVersion);
            Assert.Throws<UpdateListQuestsCountException>(() =>
                questList.UpdateListV1(
                    2,
                    _tableSheets.QuestSheet,
                    _tableSheets.QuestRewardSheet,
                    _tableSheets.QuestItemRewardSheet,
                    _tableSheets.EquipmentItemRecipeSheet));
        }

        [Fact]
        public void Migrate_Dictionary_To_List()
        {
            var questList = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            );
            questList.completedQuestIds.Add(2);
            questList.completedQuestIds.Add(1);
            var dictionary = Assert.IsType<Dictionary>(questList.SerializeDictionary());
            var list = Assert.IsType<List>(questList.SerializeList());
            var des = new QuestList(dictionary);
            var migrated = new QuestList(list);
            Assert.Equal(des.ListVersion, migrated.ListVersion);
            Assert.Equal(des.Count(), migrated.Count());
            Assert.Equal(des.completedQuestIds, migrated.completedQuestIds);
        }

        [Fact]
        public void DeserializeList()
        {
            var questList = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            );
            foreach (var quest in questList)
            {
                var serialize = (List)quest.SerializeList();
                var deserialize = Quest.DeserializeList(serialize);
                Assert.Equal(quest.Id, deserialize.Id);
                Assert.Equal(quest.Goal, deserialize.Goal);
                Assert.Equal(quest.Reward.ItemMap.OrderBy(i => i.Item1), deserialize.Reward.ItemMap.OrderBy(i => i.Item1));
                switch (deserialize)
                {
                    case CollectQuest collectQuest:
                        var cq = Assert.IsType<CollectQuest>(quest);
                        Assert.Equal(cq.ItemId, collectQuest.ItemId);
                        break;
                    case CombinationEquipmentQuest combinationEquipmentQuest:
                        var ceq = Assert.IsType<CombinationEquipmentQuest>(quest);
                        Assert.Equal(ceq.RecipeId, combinationEquipmentQuest.RecipeId);
                        Assert.Equal(ceq.StageId, combinationEquipmentQuest.StageId);
                        break;
                    case CombinationQuest combinationQuest:
                        var c = Assert.IsType<CombinationQuest>(quest);
                        Assert.Equal(c.ItemType, combinationQuest.ItemType);
                        Assert.Equal(c.ItemSubType, combinationQuest.ItemSubType);
                        break;
                    case GeneralQuest generalQuest:
                        var g = Assert.IsType<GeneralQuest>(quest);
                        Assert.Equal(g.Event, generalQuest.Event);
                        break;
                    case GoldQuest goldQuest:
                        var gq = Assert.IsType<GoldQuest>(quest);
                        Assert.Equal(gq.Type, goldQuest.Type);
                        break;
                    case ItemEnhancementQuest itemEnhancementQuest:
                        var i = Assert.IsType<ItemEnhancementQuest>(quest);
                        Assert.Equal(i.Count, itemEnhancementQuest.Count);
                        Assert.Equal(i.Grade, itemEnhancementQuest.Grade);
                        break;
                    case ItemGradeQuest itemGradeQuest:
                        var ig = Assert.IsType<ItemGradeQuest>(quest);
                        Assert.Equal(ig.Grade, itemGradeQuest.Grade);
                        Assert.Equal(ig.ItemIds, itemGradeQuest.ItemIds);
                        break;
                    case ItemTypeCollectQuest itemTypeCollectQuest:
                        var ic = Assert.IsType<ItemTypeCollectQuest>(quest);
                        Assert.Equal(ic.ItemIds, itemTypeCollectQuest.ItemIds);
                        Assert.Equal(ic.ItemType, itemTypeCollectQuest.ItemType);
                        break;
                    case MonsterQuest monsterQuest:
                        var m = Assert.IsType<MonsterQuest>(quest);
                        Assert.Equal(m.MonsterId, monsterQuest.MonsterId);
                        break;
                    case TradeQuest tradeQuest:
                        var t = Assert.IsType<TradeQuest>(quest);
                        Assert.Equal(t.Type, tradeQuest.Type);
                        break;
                    case WorldQuest _:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(quest));
                }
            }
        }
    }
}
