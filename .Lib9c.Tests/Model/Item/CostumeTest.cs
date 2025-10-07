namespace Lib9c.Tests.Model.Item
{
    using System;
    using Bencodex.Types;
    using Lib9c.Model.Item;
    using Lib9c.Model.State;
    using Lib9c.TableData.Item;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class CostumeTest
    {
        private readonly CostumeItemSheet.Row _costumeRow;

        public CostumeTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _costumeRow = tableSheets.CostumeItemSheet.First;
        }

        public static Costume CreateFirstCostume(TableSheets tableSheets, Guid guid = default)
        {
            var row = tableSheets.CostumeItemSheet.First;
            Assert.NotNull(row);

            return new Costume(row, guid == default ? Guid.NewGuid() : guid);
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_costumeRow);

            var costume = new Costume(_costumeRow, Guid.NewGuid());
            var serialized = costume.Serialize();
            var deserialized = new Costume(serialized);

            Assert.Equal(costume, deserialized);
        }

        [Fact]
        public void Serialize_ReturnsListFormat()
        {
            Assert.NotNull(_costumeRow);

            var costume = new Costume(_costumeRow, Guid.NewGuid());
            var serialized = costume.Serialize();

            Assert.IsType<List>(serialized);
        }

        [Fact]
        public void Deserialize_SupportsLegacyDictionaryFormat()
        {
            Assert.NotNull(_costumeRow);

            var costume = new Costume(_costumeRow, Guid.NewGuid());

            // Create legacy Dictionary format
            var legacySerialized = Dictionary.Empty
                .Add("id", costume.Id.Serialize())
                .Add("item_type", costume.ItemType.Serialize())
                .Add("item_sub_type", costume.ItemSubType.Serialize())
                .Add("grade", costume.Grade.Serialize())
                .Add("elemental_type", costume.ElementalType.Serialize())
                .Add("equipped", costume.Equipped.Serialize())
                .Add("spine_resource_path", costume.SpineResourcePath.Serialize())
                .Add("item_id", costume.ItemId.Serialize());

            var deserialized = new Costume(legacySerialized);

            Assert.Equal(costume.Id, deserialized.Id);
            Assert.Equal(costume.Grade, deserialized.Grade);
            Assert.Equal(costume.ItemType, deserialized.ItemType);
            Assert.Equal(costume.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(costume.ElementalType, deserialized.ElementalType);
            Assert.Equal(costume.ItemId, deserialized.ItemId);
            Assert.Equal(costume.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(costume.Equipped, deserialized.Equipped);
        }

        [Fact]
        public void Equip()
        {
            Assert.NotNull(_costumeRow);

            var costume = new Costume(_costumeRow, Guid.NewGuid());
            Assert.False(costume.Equipped);

            costume.Equip();
            Assert.True(costume.Equipped);
        }

        [Fact]
        public void Unequip()
        {
            Assert.NotNull(_costumeRow);

            var costume = new Costume(_costumeRow, Guid.NewGuid());
            costume.Equip();
            Assert.True(costume.Equipped);

            costume.Unequip();
            Assert.False(costume.Equipped);
        }

        [Fact]
        public void Update()
        {
            Assert.NotNull(_costumeRow);

            var costume = new Costume(_costumeRow, Guid.NewGuid());
            costume.Equip();
            Assert.True(costume.Equipped);

            costume.Update(1000L);
            Assert.False(costume.Equipped);
            Assert.Equal(1000L, costume.RequiredBlockIndex);
        }

        [Fact]
        public void LockThrowArgumentOutOfRangeException()
        {
            var costume = new Costume(_costumeRow, Guid.NewGuid());
            Assert.True(costume.RequiredBlockIndex >= -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => costume.Update(-1));
        }

        [Fact]
        public void SerializeWithRequiredBlockIndex()
        {
            // Check RequiredBlockIndex 0 case;
            var costume = new Costume(_costumeRow, Guid.NewGuid());
            var serialized = costume.Serialize();
            Assert.Equal(costume, new Costume(serialized));

            costume.Update(1);
            serialized = costume.Serialize();
            Assert.Equal(costume, new Costume(serialized));
        }

        [Fact]
        public void DeserializeThrowArgumentOurOfRangeException()
        {
            var costume = new Costume(_costumeRow, Guid.NewGuid());
            Assert.Equal(0, costume.RequiredBlockIndex);

            var serialized = costume.Serialize();
            // For testing negative RequiredBlockIndex, we need to create a Dictionary manually
            var dict = Dictionary.Empty
                .Add("id", costume.Id.Serialize())
                .Add("item_type", costume.ItemType.Serialize())
                .Add("item_sub_type", costume.ItemSubType.Serialize())
                .Add("grade", costume.Grade.Serialize())
                .Add("elemental_type", costume.ElementalType.Serialize())
                .Add("equipped", costume.Equipped.Serialize())
                .Add("spine_resource_path", costume.SpineResourcePath.Serialize())
                .Add("item_id", costume.ItemId.Serialize())
                .Add(RequiredBlockIndexKey, "-1");
            Assert.Throws<ArgumentOutOfRangeException>(() => new Costume(dict));
        }
    }
}
