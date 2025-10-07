namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Model.Item;
    using Lib9c.Model.State;
    using Lib9c.TableData.Item;
    using Xunit;

    public class ConsumableTest
    {
        private readonly ConsumableItemSheet.Row _consumableRow;

        public ConsumableTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _consumableRow = tableSheets.ConsumableItemSheet.First;
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_consumableRow);

            var consumable = new Consumable(_consumableRow, Guid.NewGuid(), 1000L);
            var serialized = consumable.Serialize();
            var deserialized = new Consumable(serialized);

            Assert.Equal(consumable, deserialized);
        }

        [Fact]
        public void Serialize_ReturnsListFormat()
        {
            Assert.NotNull(_consumableRow);

            var consumable = new Consumable(_consumableRow, Guid.NewGuid(), 1000L);
            var serialized = consumable.Serialize();

            Assert.IsType<List>(serialized);
        }

        [Fact]
        public void Deserialize_SupportsLegacyDictionaryFormat()
        {
            Assert.NotNull(_consumableRow);

            var consumable = new Consumable(_consumableRow, Guid.NewGuid(), 1000L);

            // Create legacy Dictionary format
            var legacySerialized = Dictionary.Empty
                .Add("id", consumable.Id.Serialize())
                .Add("item_type", consumable.ItemType.Serialize())
                .Add("item_sub_type", consumable.ItemSubType.Serialize())
                .Add("grade", consumable.Grade.Serialize())
                .Add("elemental_type", consumable.ElementalType.Serialize())
                .Add("itemId", consumable.ItemId.Serialize())
                .Add("statsMap", consumable.StatsMap.Serialize())
                .Add("skills", new List())
                .Add("buffSkills", new List())
                .Add("requiredBlockIndex", consumable.RequiredBlockIndex.Serialize())
                .Add("stats", new List(consumable.Stats.Select(s => s.SerializeWithoutAdditional())));

            var deserialized = new Consumable(legacySerialized);

            Assert.Equal(consumable.Id, deserialized.Id);
            Assert.Equal(consumable.Grade, deserialized.Grade);
            Assert.Equal(consumable.ItemType, deserialized.ItemType);
            Assert.Equal(consumable.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(consumable.ElementalType, deserialized.ElementalType);
            Assert.Equal(consumable.ItemId, deserialized.ItemId);
            Assert.Equal(consumable.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(consumable.Stats.Count, deserialized.Stats.Count);
        }

        [Fact]
        public void Update()
        {
            var consumable = new Consumable(_consumableRow, Guid.NewGuid(), 0);
            consumable.Update(10);
            Assert.Equal(10, consumable.RequiredBlockIndex);
        }
    }
}
