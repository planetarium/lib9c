namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static SerializeKeys;

    public class EquipmentTest
    {
        private readonly EquipmentItemSheet.Row _equipmentRow;

        public EquipmentTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _equipmentRow = tableSheets.EquipmentItemSheet.First;
        }

        public static Equipment CreateFirstEquipment(
            TableSheets tableSheets,
            Guid guid = default,
            long requiredBlockIndex = default)
        {
            var row = tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(row);

            return new Equipment(row, guid == default ? Guid.NewGuid() : guid, requiredBlockIndex);
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_equipmentRow);

            var equipment = new Equipment(_equipmentRow, Guid.NewGuid(), 1000L);
            var serialized = equipment.Serialize();
            var deserialized = new Equipment(serialized);

            Assert.Equal(equipment, deserialized);
        }

        [Fact]
        public void Serialize_ReturnsListFormat()
        {
            Assert.NotNull(_equipmentRow);

            var equipment = new Equipment(_equipmentRow, Guid.NewGuid(), 1000L);
            var serialized = equipment.Serialize();

            Assert.IsType<List>(serialized);
        }

        [Fact]
        public void Deserialize_SupportsLegacyDictionaryFormat()
        {
            Assert.NotNull(_equipmentRow);

            var equipment = new Equipment(_equipmentRow, Guid.NewGuid(), 1000L);

            // Create legacy Dictionary format
            var legacySerialized = Dictionary.Empty
                .Add("id", equipment.Id.Serialize())
                .Add("item_type", equipment.ItemType.Serialize())
                .Add("item_sub_type", equipment.ItemSubType.Serialize())
                .Add("grade", equipment.Grade.Serialize())
                .Add("elemental_type", equipment.ElementalType.Serialize())
                .Add("itemId", equipment.ItemId.Serialize())
                .Add("statsMap", equipment.StatsMap.Serialize())
                .Add("skills", new List())
                .Add("buffSkills", new List())
                .Add("requiredBlockIndex", equipment.RequiredBlockIndex.Serialize())
                .Add("equipped", equipment.Equipped.Serialize())
                .Add("level", equipment.level.Serialize())
                .Add("stat", equipment.Stat.SerializeForLegacyEquipmentStat())
                .Add("set_id", equipment.SetId.Serialize())
                .Add("spine_resource_path", equipment.SpineResourcePath.Serialize())
                .Add("icon_id", equipment.IconId)
                .Add("bcc", equipment.ByCustomCraft)
                .Add("cwr", equipment.CraftWithRandom)
                .Add("hroi", equipment.HasRandomOnlyIcon)
                .Add("oc", equipment.optionCountFromCombination.Serialize())
                .Add("mwmr", equipment.MadeWithMimisbrunnrRecipe.Serialize())
                .Add("eq_exp", equipment.Exp.Serialize());

            var deserialized = new Equipment(legacySerialized);

            Assert.Equal(equipment.Id, deserialized.Id);
            Assert.Equal(equipment.Grade, deserialized.Grade);
            Assert.Equal(equipment.ItemType, deserialized.ItemType);
            Assert.Equal(equipment.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(equipment.ElementalType, deserialized.ElementalType);
            Assert.Equal(equipment.ItemId, deserialized.ItemId);
            Assert.Equal(equipment.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(equipment.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(equipment.SetId, deserialized.SetId);
            Assert.Equal(equipment.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(equipment.Equipped, deserialized.Equipped);
            Assert.Equal(equipment.level, deserialized.level);
            Assert.Equal(equipment.Exp, deserialized.Exp);
        }

        [Fact]
        public void Equip()
        {
            Assert.NotNull(_equipmentRow);

            var equipment = new Equipment(_equipmentRow, Guid.NewGuid(), 1000L);
            Assert.False(equipment.Equipped);

            equipment.Equip();
            Assert.True(equipment.Equipped);
        }

        [Fact]
        public void Unequip()
        {
            Assert.NotNull(_equipmentRow);

            var equipment = new Equipment(_equipmentRow, Guid.NewGuid(), 1000L);
            equipment.Equip();
            Assert.True(equipment.Equipped);

            equipment.Unequip();
            Assert.False(equipment.Equipped);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(14176747520000)] // Max exp. of equipment
        public void Serialize_WithExp(long exp)
        {
            Assert.NotNull(_equipmentRow);

            var costume = new Equipment(_equipmentRow, Guid.NewGuid(), 0);
            costume.Exp = exp;
            var serialized = costume.Serialize();
            var deserialized = new Equipment(serialized);
            var reSerialized = deserialized.Serialize();

            Assert.Equal((Integer)exp, ((List)serialized)[22]);
            Assert.Equal(costume, deserialized);
            Assert.Equal(serialized, reSerialized);
        }

        [Fact]
        public void LevelUp()
        {
            var row = new EquipmentItemSheet.Row();
            row.Set(new List<string>() { "10100000", "Weapon", "0", "Normal", "0", "ATK", "1", "2", "10100000", });
            var equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, 0, 0);

            Assert.Equal(1m, equipment.StatsMap.ATK);
            equipment.LevelUpV1();
            Assert.Equal(2m, equipment.StatsMap.ATK);
        }

        [Fact]
        public void GetHammerExp()
        {
            var costSheet = new EnhancementCostSheetV3();
            costSheet.Set(
                @"600301,EquipmentMaterial,1,0,0,100,0,0,0,0,0,0,0,0,0
600302,EquipmentMaterial,2,0,0,200,0,0,0,0,0,0,0,0,0
600303,EquipmentMaterial,3,0,0,300,0,0,0,0,0,0,0,0,0
600304,EquipmentMaterial,4,0,0,400,0,0,0,0,0,0,0,0,0
600305,EquipmentMaterial,5,0,0,500,0,0,0,0,0,0,0,0,0
");
            foreach (var costRow in costSheet.Values)
            {
                var id = costRow.Id;
                Assert.True(costRow.Exp > 0L);
                Assert.Equal(costRow.Exp, costSheet.GetHammerExp(id));
            }
        }

        [Fact]
        public void Serialize_PreservesPotential()
        {
            Assert.NotNull(_equipmentRow);

            var equipment = new Equipment(_equipmentRow, Guid.NewGuid(), 1000L);
            var potential = new EquipmentPotential(
                2,
                new List<PotentialOptionSlot>
                {
                    new PotentialOptionSlot(700001, 12m),
                    new PotentialOptionSlot(700002, 3.5m),
                });
            equipment.SetPotential(potential);

            var serialized = equipment.Serialize();
            var deserialized = new Equipment(serialized);

            Assert.Equal(potential, deserialized.Potential);
            Assert.Equal(serialized, deserialized.Serialize());
        }

        [Fact]
        public void Serialize_DefaultPotentialIsEmpty()
        {
            Assert.NotNull(_equipmentRow);

            var equipment = new Equipment(_equipmentRow, Guid.NewGuid(), 1000L);
            Assert.True(equipment.Potential.IsEmpty);

            // The potential layer occupies the trailing index 23.
            var serialized = (List)equipment.Serialize();
            Assert.Equal(24, serialized.Count);

            var deserialized = new Equipment(serialized);
            Assert.True(deserialized.Potential.IsEmpty);
        }

        [Fact]
        public void Deserialize_LegacyListWithoutPotential_DefaultsToEmpty()
        {
            Assert.NotNull(_equipmentRow);

            var equipment = new Equipment(_equipmentRow, Guid.NewGuid(), 1000L);
            var full = (List)equipment.Serialize();

            // Simulate equipment serialized before the potential layer was introduced:
            // drop the trailing field so only indices 0~22 remain.
            var legacy = new List(full.Take(23));
            Assert.Equal(23, legacy.Count);

            var deserialized = new Equipment(legacy);
            Assert.True(deserialized.Potential.IsEmpty);
            Assert.Equal(equipment.ItemId, deserialized.ItemId);
            Assert.Equal(equipment.Exp, deserialized.Exp);
        }

        [Fact]
        public void Deserialize_LegacyDictionary_DefaultsToEmptyPotential()
        {
            Assert.NotNull(_equipmentRow);

            var equipment = new Equipment(_equipmentRow, Guid.NewGuid(), 1000L);
            var legacySerialized = Dictionary.Empty
                .Add("id", equipment.Id.Serialize())
                .Add("item_type", equipment.ItemType.Serialize())
                .Add("item_sub_type", equipment.ItemSubType.Serialize())
                .Add("grade", equipment.Grade.Serialize())
                .Add("elemental_type", equipment.ElementalType.Serialize())
                .Add("itemId", equipment.ItemId.Serialize())
                .Add("statsMap", equipment.StatsMap.Serialize())
                .Add("skills", new List())
                .Add("buffSkills", new List())
                .Add("requiredBlockIndex", equipment.RequiredBlockIndex.Serialize())
                .Add("equipped", equipment.Equipped.Serialize())
                .Add("level", equipment.level.Serialize())
                .Add("stat", equipment.Stat.SerializeForLegacyEquipmentStat())
                .Add("set_id", equipment.SetId.Serialize())
                .Add("spine_resource_path", equipment.SpineResourcePath.Serialize());

            var deserialized = new Equipment(legacySerialized);

            Assert.True(deserialized.Potential.IsEmpty);
        }

        [Fact]
        public void ItemFactory_RoundTrip_PreservesPotential_AllEquipmentSubTypes()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var subTypes = tableSheets.EquipmentItemSheet.OrderedList
                .Select(r => r.ItemSubType)
                .Distinct()
                .ToList();
            Assert.NotEmpty(subTypes);

            foreach (var subType in subTypes)
            {
                var row = tableSheets.EquipmentItemSheet.OrderedList
                    .First(r => r.ItemSubType == subType);
                var equipment = (Equipment)ItemFactory.CreateItemUsable(
                    row, Guid.NewGuid(), 1000L);
                var potential = new EquipmentPotential(
                    2,
                    new List<PotentialOptionSlot>
                    {
                        new PotentialOptionSlot(700001, 12m),
                        new PotentialOptionSlot(700002, 3.5m),
                    });
                equipment.SetPotential(potential);

                var serialized = equipment.Serialize();

                // Go through the production deserialization entry point,
                // which dispatches to the concrete subclass (Weapon/Armor/... ).
                var deserialized = ItemFactory.Deserialize(serialized);

                var deserializedEquipment = Assert.IsAssignableFrom<Equipment>(deserialized);
                Assert.Equal(subType, deserializedEquipment.ItemSubType);
                Assert.Equal(potential, deserializedEquipment.Potential);
                Assert.Equal(serialized, deserializedEquipment.Serialize());
            }
        }

        [Fact]
        public void ItemFactory_LegacyListWithoutPotential_DefaultsToEmpty_AllEquipmentSubTypes()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var subTypes = tableSheets.EquipmentItemSheet.OrderedList
                .Select(r => r.ItemSubType)
                .Distinct()
                .ToList();
            Assert.NotEmpty(subTypes);

            foreach (var subType in subTypes)
            {
                var row = tableSheets.EquipmentItemSheet.OrderedList
                    .First(r => r.ItemSubType == subType);
                var equipment = (Equipment)ItemFactory.CreateItemUsable(
                    row, Guid.NewGuid(), 1000L);

                // Simulate on-chain data serialized before the potential layer:
                // drop the trailing field so only indices 0~22 remain.
                var legacy = new List(((List)equipment.Serialize()).Take(23));
                Assert.Equal(23, legacy.Count);

                var deserialized = (Equipment)ItemFactory.Deserialize(legacy);
                Assert.Equal(subType, deserialized.ItemSubType);
                Assert.True(deserialized.Potential.IsEmpty);

                // Re-serializing legacy data yields the current (24-field) format with an empty potential.
                Assert.Equal(24, ((List)deserialized.Serialize()).Count);
            }
        }
    }
}
