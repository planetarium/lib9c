namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Linq;
    using Lib9c.Arguments;
    using Lib9c.Tests.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class ItemFactoryTest
    {
        [Fact]
        public void CreateItem()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var itemSheet = tableSheets.ItemSheet;
            foreach (var itemType in Enum.GetValues<ItemType>())
            {
                var row = itemSheet.OrderedList!.First(e => e.ItemType == itemType);
                Validate(row);
                row = itemSheet.OrderedList.Last(e => e.ItemType == itemType);
                Validate(row);
            }

            void Validate(ItemSheet.Row row)
            {
                var itemArgs = new ItemArgs(sheetId: row.Id);
                var itemWithRow = ItemFactory.CreateItem(row, new TestRandom());
                var itemWithArgs = ItemFactory.CreateItem(itemArgs, itemSheet, new TestRandom());
                Assert.Equal(itemWithRow.Serialize(), itemWithArgs.Serialize());
            }
        }
    }
}
