using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Lib9c.Model.Item;
using Lib9c.TableData.Item;
using Xunit;

namespace Lib9c.Tests.Model.Item
{
    public class ArmorTest
    {
        private readonly EquipmentItemSheet.Row _armorRow;

        public ArmorTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _armorRow = tableSheets.EquipmentItemSheet.OrderedList.FirstOrDefault(row =>
                row.ItemSubType == ItemSubType.Armor);
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_armorRow);

            var costume = new Armor(_armorRow, Guid.NewGuid(), 0);
            var serialized = costume.Serialize();
            var deserialized = new Armor((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(costume, deserialized);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            Assert.NotNull(_armorRow);

            var costume = new Armor(_armorRow, Guid.NewGuid(), 0);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, costume);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (Armor)formatter.Deserialize(ms);

            Assert.Equal(costume, deserialized);
        }
    }
}
