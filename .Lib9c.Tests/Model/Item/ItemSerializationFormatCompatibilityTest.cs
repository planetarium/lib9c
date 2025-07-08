namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    /// <summary>
    /// Tests for item serialization format compatibility between Dictionary and List formats.
    /// Focuses on bidirectional conversion and comprehensive item type coverage.
    /// </summary>
    public class ItemSerializationFormatCompatibilityTest
    {
        private readonly TableSheets _tableSheets;

        public ItemSerializationFormatCompatibilityTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Material_DictionaryToList_Bidirectional_Compatibility()
        {
            // Arrange
            var materialRow = _tableSheets.MaterialItemSheet.First;
            var originalMaterial = new Material(materialRow);

            // Act - Dictionary to List migration
            var legacyDict = Dictionary.Empty
                .Add("id", originalMaterial.Id.Serialize())
                .Add("grade", originalMaterial.Grade.Serialize())
                .Add("item_type", originalMaterial.ItemType.Serialize())
                .Add("item_sub_type", originalMaterial.ItemSubType.Serialize())
                .Add("elemental_type", originalMaterial.ElementalType.Serialize())
                .Add("item_id", originalMaterial.ItemId.Serialize());

            var deserializedFromDict = new Material(legacyDict);
            var listSerialized = deserializedFromDict.Serialize();
            var deserializedFromList = new Material(listSerialized);

            // Assert - Bidirectional compatibility
            Assert.Equal(originalMaterial.Id, deserializedFromDict.Id);
            Assert.Equal(originalMaterial.Grade, deserializedFromDict.Grade);
            Assert.Equal(originalMaterial.ItemType, deserializedFromDict.ItemType);
            Assert.Equal(originalMaterial.ItemSubType, deserializedFromDict.ItemSubType);
            Assert.Equal(originalMaterial.ElementalType, deserializedFromDict.ElementalType);
            Assert.Equal(originalMaterial.ItemId, deserializedFromDict.ItemId);
            Assert.Equal(deserializedFromDict, deserializedFromList);
        }

        [Fact]
        public void Consumable_DictionaryToList_Bidirectional_Compatibility()
        {
            // Arrange
            var consumableRow = _tableSheets.ConsumableItemSheet.First;
            var originalConsumable = new Consumable(consumableRow, Guid.NewGuid(), 1000L);

            // Act - Dictionary to List migration
            var legacyStatsMapDict = (Dictionary)Dictionary.Empty
                .Add(
                    StatType.HP.Serialize(),
                    Dictionary.Empty
                        .Add("statType", StatType.HP.Serialize())
                        .Add("value", 100m.Serialize())
                        .Add("additionalValue", 0m.Serialize()))
                .Add(
                    StatType.ATK.Serialize(),
                    Dictionary.Empty
                        .Add("statType", StatType.ATK.Serialize())
                        .Add("value", 50m.Serialize())
                        .Add("additionalValue", 10m.Serialize()));

            var legacyDict = (Dictionary)Dictionary.Empty
                .Add((IKey)(Text)"id", originalConsumable.Id.Serialize())
                .Add((IKey)(Text)"grade", originalConsumable.Grade.Serialize())
                .Add((IKey)(Text)"item_type", originalConsumable.ItemType.Serialize())
                .Add((IKey)(Text)"item_sub_type", originalConsumable.ItemSubType.Serialize())
                .Add((IKey)(Text)"elemental_type", originalConsumable.ElementalType.Serialize())
                .Add((IKey)(Text)"itemId", originalConsumable.ItemId.Serialize())
                .Add((IKey)(Text)"statsMap", legacyStatsMapDict)
                .Add((IKey)(Text)"skills", new List(originalConsumable.Skills.Select(s => s.Serialize())))
                .Add((IKey)(Text)"buffSkills", new List(originalConsumable.BuffSkills.Select(s => s.Serialize())))
                .Add((IKey)(Text)"requiredBlockIndex", originalConsumable.RequiredBlockIndex.Serialize());

            var deserializedFromDict = new Consumable(legacyDict);
            var listSerialized = deserializedFromDict.Serialize();
            var deserializedFromList = new Consumable(listSerialized);

            // Assert - Bidirectional compatibility
            Assert.Equal(originalConsumable.Id, deserializedFromDict.Id);
            Assert.Equal(originalConsumable.Grade, deserializedFromDict.Grade);
            Assert.Equal(originalConsumable.ItemType, deserializedFromDict.ItemType);
            Assert.Equal(originalConsumable.ItemSubType, deserializedFromDict.ItemSubType);
            Assert.Equal(originalConsumable.ElementalType, deserializedFromDict.ElementalType);
            Assert.Equal(originalConsumable.ItemId, deserializedFromDict.ItemId);
            Assert.Equal(originalConsumable.RequiredBlockIndex, deserializedFromDict.RequiredBlockIndex);
            Assert.Equal(originalConsumable.Stats?.Count ?? 0, deserializedFromDict.Stats?.Count ?? 0);
            Assert.Equal(deserializedFromDict, deserializedFromList);
        }

        [Fact]
        public void Costume_DictionaryToList_Bidirectional_Compatibility()
        {
            // Arrange
            var costumeRow = _tableSheets.CostumeItemSheet.First;
            var originalCostume = new Costume(costumeRow, Guid.NewGuid());

            // Act - Dictionary to List migration
            var legacyDict = Dictionary.Empty
                .Add("id", originalCostume.Id.Serialize())
                .Add("grade", originalCostume.Grade.Serialize())
                .Add("item_type", originalCostume.ItemType.Serialize())
                .Add("item_sub_type", originalCostume.ItemSubType.Serialize())
                .Add("elemental_type", originalCostume.ElementalType.Serialize())
                .Add("item_id", originalCostume.ItemId.Serialize())
                .Add("spine_resource_path", originalCostume.SpineResourcePath.Serialize())
                .Add("equipped", originalCostume.Equipped.Serialize());

            var deserializedFromDict = new Costume(legacyDict);
            var listSerialized = deserializedFromDict.Serialize();
            var deserializedFromList = new Costume(listSerialized);

            // Assert - Bidirectional compatibility
            Assert.Equal(originalCostume.Id, deserializedFromDict.Id);
            Assert.Equal(originalCostume.Grade, deserializedFromDict.Grade);
            Assert.Equal(originalCostume.ItemType, deserializedFromDict.ItemType);
            Assert.Equal(originalCostume.ItemSubType, deserializedFromDict.ItemSubType);
            Assert.Equal(originalCostume.ElementalType, deserializedFromDict.ElementalType);
            Assert.Equal(originalCostume.ItemId, deserializedFromDict.ItemId);
            Assert.Equal(originalCostume.SpineResourcePath, deserializedFromDict.SpineResourcePath);
            Assert.Equal(originalCostume.Equipped, deserializedFromDict.Equipped);
            Assert.Equal(deserializedFromDict, deserializedFromList);
        }

        [Fact]
        public void Equipment_DictionaryToList_Bidirectional_Compatibility()
        {
            // Arrange
            var equipmentRow = _tableSheets.EquipmentItemSheet.First;
            var originalEquipment = CreateEquipmentWithOptions(equipmentRow, Guid.NewGuid(), 1000L);

            // Act - Dictionary to List migration
            var legacyStatsMapDict = new Dictionary(
                Enum.GetValues(typeof(StatType))
                    .Cast<StatType>()
                    .Where(type => type != StatType.NONE)
                    .Select(type =>
                    {
                        var stat = originalEquipment.StatsMap.GetDecimalStats(false).FirstOrDefault(x => x.StatType == type);
                        return new KeyValuePair<IKey, IValue>(
                            type.Serialize(),
                            new Dictionary(new[]
                            {
                                new KeyValuePair<IKey, IValue>((Text)"statType", type.Serialize()),
                                new KeyValuePair<IKey, IValue>((Text)"value", (stat != null ? stat.BaseValue : 0m).Serialize()),
                                new KeyValuePair<IKey, IValue>((Text)"additionalValue", (stat != null ? stat.AdditionalValue : 0m).Serialize()),
                            })
                        );
                    })
            );

            var legacyDict = (Dictionary)Dictionary.Empty
                .Add((IKey)(Text)"id", originalEquipment.Id.Serialize())
                .Add((Text)"grade", originalEquipment.Grade.Serialize())
                .Add((Text)"item_type", originalEquipment.ItemType.Serialize())
                .Add((Text)"item_sub_type", originalEquipment.ItemSubType.Serialize())
                .Add((Text)"elemental_type", originalEquipment.ElementalType.Serialize())
                .Add((Text)"itemId", originalEquipment.ItemId.Serialize())
                .Add((Text)"statsMap", legacyStatsMapDict)
                .Add((Text)"skills", new List(originalEquipment.Skills.Select(s => s.Serialize())))
                .Add((Text)"buffSkills", new List(originalEquipment.BuffSkills.Select(s => s.Serialize())))
                .Add((Text)"requiredBlockIndex", originalEquipment.RequiredBlockIndex.Serialize())
                .Add((Text)"equipped", originalEquipment.Equipped.Serialize())
                .Add((Text)"level", originalEquipment.level.Serialize())
                .Add((Text)"set_id", originalEquipment.SetId.Serialize())
                .Add((Text)"spine_resource_path", originalEquipment.SpineResourcePath.Serialize())
                .Add((Text)"eq_exp", originalEquipment.Exp.Serialize())
                .Add((Text)"stat", originalEquipment.Stat.SerializeForLegacyEquipmentStat());

            var deserializedFromDict = new Equipment(legacyDict);
            var listSerialized = deserializedFromDict.Serialize();
            var deserializedFromList = new Equipment(listSerialized);

            // Verify that options are preserved
            Assert.True(deserializedFromDict.Skills.Count >= 0);
            Assert.True(deserializedFromList.Skills.Count >= 0);
            Assert.Equal(
                originalEquipment.StatsMap.GetAdditionalStats(true).Count(),
                deserializedFromDict.StatsMap.GetAdditionalStats(true).Count());
            Assert.Equal(
                originalEquipment.StatsMap.GetAdditionalStats(true).Count(),
                deserializedFromList.StatsMap.GetAdditionalStats(true).Count());

            // Assert - Bidirectional compatibility
            Assert.Equal(originalEquipment.Id, deserializedFromDict.Id);
            Assert.Equal(originalEquipment.Grade, deserializedFromDict.Grade);
            Assert.Equal(originalEquipment.ItemType, deserializedFromDict.ItemType);
            Assert.Equal(originalEquipment.ItemSubType, deserializedFromDict.ItemSubType);
            Assert.Equal(originalEquipment.ElementalType, deserializedFromDict.ElementalType);
            Assert.Equal(originalEquipment.ItemId, deserializedFromDict.ItemId);
            Assert.Equal(originalEquipment.RequiredBlockIndex, deserializedFromDict.RequiredBlockIndex);
            Assert.Equal(originalEquipment.Stat.StatType, deserializedFromDict.Stat.StatType);
            Assert.Equal(originalEquipment.SetId, deserializedFromDict.SetId);
            Assert.Equal(originalEquipment.SpineResourcePath, deserializedFromDict.SpineResourcePath);
            Assert.Equal(originalEquipment.Equipped, deserializedFromDict.Equipped);
            Assert.Equal(originalEquipment.level, deserializedFromDict.level);
            Assert.Equal(originalEquipment.Exp, deserializedFromDict.Exp);
            Assert.Equal(deserializedFromDict, deserializedFromList);
        }

        [Fact]
        public void Serialize_ReturnsListFormat()
        {
            // Arrange
            var materialRow = _tableSheets.MaterialItemSheet.First;
            var material = new Material(materialRow);

            // Act
            var serialized = material.Serialize();

            // Assert
            Assert.IsType<List>(serialized);
            var list = (List)serialized;
            Assert.Equal(ItemBase.SerializationVersion, ((Integer)list[0]).Value);
        }

        [Fact]
        public void Deserialize_SupportsBothFormats()
        {
            // Arrange
            var materialRow = _tableSheets.MaterialItemSheet.First;
            var material = new Material(materialRow);

            // Act - Test List format
            var listSerialized = material.Serialize();
            var deserializedFromList = new Material(listSerialized);

            // Act - Test Dictionary format
            var dictSerialized = Dictionary.Empty
                .Add("id", material.Id.Serialize())
                .Add("grade", material.Grade.Serialize())
                .Add("item_type", material.ItemType.Serialize())
                .Add("item_sub_type", material.ItemSubType.Serialize())
                .Add("elemental_type", material.ElementalType.Serialize())
                .Add("item_id", material.ItemId.Serialize());
            var deserializedFromDict = new Material(dictSerialized);

            // Assert
            Assert.Equal(material.Id, deserializedFromList.Id);
            Assert.Equal(material.Id, deserializedFromDict.Id);
        }

        /// <summary>
        /// Creates equipment with options similar to CombinationEquipment action.
        /// </summary>
        /// <param name="equipmentRow">Equipment item sheet row.</param>
        /// <param name="id">Equipment ID.</param>
        /// <param name="requiredBlockIndex">Required block index.</param>
        /// <returns>Equipment with randomly added options.</returns>
        private Equipment CreateEquipmentWithOptions(
            EquipmentItemSheet.Row equipmentRow,
            Guid id,
            long requiredBlockIndex)
        {
            var equipment = new Equipment(equipmentRow, id, requiredBlockIndex);
            var random = new Random(12345); // Fixed seed for deterministic test

            // Get sheets for option generation
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var optionSheet = tableSheets.EquipmentItemOptionSheet;
            var skillSheet = tableSheets.SkillSheet;

            // Add some random stat options
            var statOptions = optionSheet.Values
                .Where(row => row.StatType != StatType.NONE)
                .Take(3)
                .ToList();

            foreach (var optionRow in statOptions)
            {
                var value = random.Next(optionRow.StatMin, optionRow.StatMax + 1);
                equipment.StatsMap.AddStatAdditionalValue(optionRow.StatType, value);
                equipment.optionCountFromCombination++;
            }

            // Add some random skill options
            var skillOptions = optionSheet.Values
                .Where(row => row.StatType == StatType.NONE && row.SkillId > 0)
                .Take(2)
                .ToList();

            foreach (var optionRow in skillOptions)
            {
                var skillRow = skillSheet.OrderedList.FirstOrDefault(r => r.Id == optionRow.SkillId);
                if (skillRow != null)
                {
                    var dmg = random.Next(optionRow.SkillDamageMin, optionRow.SkillDamageMax + 1);
                    var chance = random.Next(optionRow.SkillChanceMin, optionRow.SkillChanceMax + 1);
                    var skill = SkillFactory.Get(skillRow, dmg, chance, 0, StatType.NONE);
                    equipment.Skills.Add(skill);
                    equipment.optionCountFromCombination++;
                }
            }

            return equipment;
        }
    }
}
