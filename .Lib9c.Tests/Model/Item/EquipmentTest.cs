namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Lib9c.Model.Item;
    using Lib9c.Model.State;
    using Lib9c.TableData.Cost;
    using Lib9c.TableData.Item;
    using Xunit;

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
    }
}
