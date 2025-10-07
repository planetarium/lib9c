namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Linq;
    using Lib9c.Model.Item;
    using Lib9c.TableData.Item;
    using Xunit;

    public class ArmorTest
    {
        private readonly EquipmentItemSheet.Row _armorRow;

        public ArmorTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _armorRow = tableSheets.EquipmentItemSheet.OrderedList.FirstOrDefault(
                row =>
                    row.ItemSubType == ItemSubType.Armor);
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_armorRow);

            var costume = new Armor(_armorRow, Guid.NewGuid(), 0);
            var serialized = costume.Serialize();
            var deserialized = new Armor(serialized);

            Assert.Equal(costume, deserialized);
        }
    }
}
