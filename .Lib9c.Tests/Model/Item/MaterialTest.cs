namespace Lib9c.Tests.Model.Item
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Lib9c.Model.Item;
    using Lib9c.Model.State;
    using Lib9c.TableData.Item;
    using Xunit;

    public class MaterialTest
    {
        private readonly MaterialItemSheet.Row _materialRow;

        public MaterialTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _materialRow = tableSheets.MaterialItemSheet.First;
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_materialRow);

            var material = new Material(_materialRow);
            var serialized = material.Serialize();
            var deserialized = new Material(serialized);

            Assert.Equal(material, deserialized);
        }

        [Fact]
        public void SerializeWithDotNetApi()
        {
            Assert.NotNull(_materialRow);

            var material = new Material(_materialRow);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, material);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (Material)formatter.Deserialize(ms);

            Assert.Equal(material, deserialized);
        }

        [Fact]
        public void Serialize_ReturnsListFormat()
        {
            Assert.NotNull(_materialRow);

            var material = new Material(_materialRow);
            var serialized = material.Serialize();

            Assert.IsType<Bencodex.Types.List>(serialized);
        }

        [Fact]
        public void Deserialize_SupportsLegacyDictionaryFormat()
        {
            Assert.NotNull(_materialRow);

            var material = new Material(_materialRow);

            // Create legacy Dictionary format
            var legacySerialized = Bencodex.Types.Dictionary.Empty
                .Add("id", material.Id.Serialize())
                .Add("item_type", material.ItemType.Serialize())
                .Add("item_sub_type", material.ItemSubType.Serialize())
                .Add("grade", material.Grade.Serialize())
                .Add("elemental_type", material.ElementalType.Serialize())
                .Add("item_id", material.ItemId.Serialize());

            var deserialized = new Material(legacySerialized);

            Assert.Equal(material.Id, deserialized.Id);
            Assert.Equal(material.Grade, deserialized.Grade);
            Assert.Equal(material.ItemType, deserialized.ItemType);
            Assert.Equal(material.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(material.ElementalType, deserialized.ElementalType);
            Assert.Equal(material.ItemId, deserialized.ItemId);
        }
    }
}
