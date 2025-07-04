namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Formatters;
    using MessagePack;
    using MessagePack.Resolvers;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    /// <summary>
    /// Tests for item serialization error handling and edge cases.
    /// Focuses on exception handling, invalid formats, and insufficient fields.
    /// </summary>
    public class ItemSerializationErrorHandlingTest
    {
        private readonly MaterialItemSheet.Row _materialRow;
        private readonly ConsumableItemSheet.Row _consumableRow;
        private readonly EquipmentItemSheet.Row _equipmentRow;

        public ItemSerializationErrorHandlingTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _materialRow = tableSheets.MaterialItemSheet.First;
            _consumableRow = tableSheets.ConsumableItemSheet.First;
            _equipmentRow = tableSheets.EquipmentItemSheet.First;

            // Setup MessagePack resolver
            var resolver = CompositeResolver.Create(
                NineChroniclesResolver.Instance,
                StandardResolver.Instance
            );
            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePackSerializer.DefaultOptions = options;
        }

        [Fact]
        public void Material_Serialize_With_MessagePack()
        {
            // Arrange
            var material = new Material(_materialRow);

            // Act - Dictionary format
            var serializedDict = material.Serialize();
            var deserializedDict = new Material(serializedDict);

            // Act - List format (new format)
            var serializedList = material.Serialize();
            var deserializedList = new Material(serializedList);

            // Assert
            Assert.Equal(material.Id, deserializedDict.Id);
            Assert.Equal(material.ItemId, deserializedDict.ItemId);
            Assert.Equal(material.Id, deserializedList.Id);
            Assert.Equal(material.ItemId, deserializedList.ItemId);
        }

        [Fact]
        public void Consumable_Serialize_With_MessagePack()
        {
            // Arrange
            var consumable = new Consumable(_consumableRow, Guid.NewGuid(), 1000L);

            // Act - Dictionary format
            var serializedDict = consumable.Serialize();
            var deserializedDict = new Consumable(serializedDict);

            // Act - List format (new format)
            var serializedList = consumable.Serialize();
            var deserializedList = new Consumable(serializedList);

            // Assert
            Assert.Equal(consumable.Id, deserializedDict.Id);
            Assert.Equal(consumable.ItemId, deserializedDict.ItemId);
            Assert.Equal(consumable.Id, deserializedList.Id);
            Assert.Equal(consumable.ItemId, deserializedList.ItemId);
        }

        [Fact]
        public void Equipment_Serialize_With_MessagePack()
        {
            // Arrange
            var equipment = new Equipment(_equipmentRow, Guid.NewGuid(), 1000L);

            // Act - Dictionary format
            var serializedDict = equipment.Serialize();
            var deserializedDict = new Equipment(serializedDict);

            // Act - List format (new format)
            var serializedList = equipment.Serialize();
            var deserializedList = new Equipment(serializedList);

            // Assert
            Assert.Equal(equipment.Id, deserializedDict.Id);
            Assert.Equal(equipment.ItemId, deserializedDict.ItemId);
            Assert.Equal(equipment.Id, deserializedList.Id);
            Assert.Equal(equipment.ItemId, deserializedList.ItemId);
        }

        [Fact]
        public void Item_Backward_Compatibility_Dictionary_Format()
        {
            // Arrange - Create legacy Dictionary format
            var material = new Material(_materialRow);
            var legacyDict = Dictionary.Empty
                .Add("id", material.Id.Serialize())
                .Add("grade", material.Grade.Serialize())
                .Add("item_type", ((int)material.ItemType).Serialize())
                .Add("item_sub_type", ((int)material.ItemSubType).Serialize())
                .Add("elemental_type", ((int)material.ElementalType).Serialize())
                .Add("item_id", material.ItemId.Serialize());

            // Act
            var deserialized = new Material(legacyDict);

            // Assert
            Assert.Equal(material.Id, deserialized.Id);
            Assert.Equal(material.Grade, deserialized.Grade);
            Assert.Equal(material.ItemType, deserialized.ItemType);
            Assert.Equal(material.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(material.ElementalType, deserialized.ElementalType);
            Assert.Equal(material.ItemId, deserialized.ItemId);
        }

        [Fact]
        public void Item_Forward_Compatibility_List_Format()
        {
            // Arrange - Create new List format
            var material = new Material(_materialRow);
            var newList = List.Empty
                .Add(ItemBase.SerializationVersion)
                .Add(material.Id.Serialize())
                .Add(((int)material.ItemType).ToString().Serialize())
                .Add(((int)material.ItemSubType).ToString().Serialize())
                .Add(material.Grade.Serialize())
                .Add(((int)material.ElementalType).ToString().Serialize())
                .Add(material.ItemId.Serialize());

            // Act
            var deserialized = new Material(newList);

            // Assert
            Assert.Equal(material.Id, deserialized.Id);
            Assert.Equal(material.Grade, deserialized.Grade);
            Assert.Equal(material.ItemType, deserialized.ItemType);
            Assert.Equal(material.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(material.ElementalType, deserialized.ElementalType);
            Assert.Equal(material.ItemId, deserialized.ItemId);
        }

        [Fact]
        public void Item_Serialize_With_DotNet_API()
        {
            // Arrange
            var material = new Material(_materialRow);

            // Act
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, material);
            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (Material)formatter.Deserialize(ms);

            // Assert
            Assert.Equal(material.Id, deserialized.Id);
            Assert.Equal(material.ItemId, deserialized.ItemId);
        }

        [Fact]
        public void Item_Invalid_Serialization_Format_Throws_Exception()
        {
            // Arrange - Invalid format (Text instead of Dictionary or List)
            var invalidFormat = (Text)"invalid";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Material(invalidFormat));
            Assert.Contains("Unsupported serialization format", exception.Message);
        }

        [Fact]
        public void Item_Insufficient_Fields_Throws_Exception()
        {
            // Arrange - List with insufficient fields
            var insufficientList = List.Empty
                .Add(ItemBase.SerializationVersion)
                .Add(1.Serialize())
                .Add(2.Serialize())
                .Add(3.Serialize())
                .Add(4.Serialize())
                .Add(5.Serialize()); // Only 6 fields, need at least 7

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Material(insufficientList));
            Assert.Contains("Invalid list length for Material", exception.Message);
            Assert.Contains("expected at least 7", exception.Message);
            Assert.Contains("Fields:", exception.Message);
        }
    }
}
