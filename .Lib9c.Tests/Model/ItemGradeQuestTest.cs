namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Model.Item;
    using Lib9c.Model.Quest;
    using Xunit;

    public class ItemGradeQuestTest
    {
        private readonly TableSheets _tableSheets;

        public ItemGradeQuestTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize()
        {
            var quest = new ItemGradeQuest(
                _tableSheets.ItemGradeQuestSheet.Values.First(),
                new QuestReward(new Dictionary<int, int>())
            );

            var expected = new List<int>()
            {
                10110000,
                10111000,
            };

            foreach (var itemId in expected.OrderByDescending(i => i))
            {
                var item = ItemFactory.CreateItemUsable(_tableSheets.EquipmentItemSheet[itemId], default, 0);
                quest.Update(item);
            }

            Assert.Equal(expected, quest.ItemIds);

            var des = new ItemGradeQuest((Dictionary)quest.Serialize());

            Assert.Equal(expected, des.ItemIds);
        }
    }
}
