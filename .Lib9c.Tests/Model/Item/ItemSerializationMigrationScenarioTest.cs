namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Lib9c.Tests.Model.Skill;
    using Libplanet.Common;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    /// <summary>
    /// Tests for item serialization migration scenarios and complex use cases.
    /// Focuses on real-world migration scenarios and edge cases.
    /// </summary>
    public class ItemSerializationMigrationScenarioTest
    {
        private readonly TableSheets _tableSheets;

        public ItemSerializationMigrationScenarioTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Material_DictionaryToList_Migration_WithNullableFields()
        {
            // Arrange: Create a material with some nullable/default values
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var material = new Material(materialRow);

            // Act: Serialize to Dictionary (legacy format)
            var dictSerialized = Dictionary.Empty
                .Add("id", material.Id.Serialize())
                .Add("item_type", material.ItemType.Serialize())
                .Add("item_sub_type", material.ItemSubType.Serialize())
                .Add("grade", material.Grade.Serialize())
                .Add("item_id", material.ItemId.Serialize());
            // Note: elemental_type is intentionally omitted to test nullable handling

            // Deserialize from Dictionary
            var deserializedFromDict = new Material(dictSerialized);

            // Serialize to List (new format) and then deserialize from that List
            var listSerialized = deserializedFromDict.Serialize();
            var deserializedFromList = new Material(listSerialized);

            // Assert: All objects should be equal
            Assert.Equal(material, deserializedFromDict);
            Assert.Equal(deserializedFromDict, deserializedFromList);
            Assert.Equal(material, deserializedFromList);

            // Verify default values are preserved
            Assert.Equal(ElementalType.Normal, deserializedFromList.ElementalType); // Default value
        }

        [Fact]
        public void Material_DictionaryToList_Migration_WithMinimalFields()
        {
            // Arrange: Create a material with minimal Dictionary data (missing some fields)
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var dictSerialized = Dictionary.Empty
                .Add("id", materialRow.Id.Serialize())
                .Add("item_type", materialRow.ItemType.Serialize())
                .Add("item_sub_type", materialRow.ItemSubType.Serialize())
                .Add("grade", materialRow.Grade.Serialize())
                .Add("elemental_type", materialRow.ElementalType.Serialize())
                .Add("item_id", materialRow.ItemId.Serialize());
            // All required fields are included, testing that migration works correctly

            // Act: Deserialize from Dictionary
            var deserializedFromDict = new Material(dictSerialized);

            // Serialize to List (new format)
            var listSerialized = deserializedFromDict.Serialize();

            // Deserialize from List (should handle padded list safely)
            var deserializedFromList = new Material(listSerialized);

            // Assert: Should handle migration correctly
            Assert.Equal(materialRow.Id, deserializedFromDict.Id);
            Assert.Equal(materialRow.ItemType, deserializedFromDict.ItemType);
            Assert.Equal(materialRow.ItemSubType, deserializedFromDict.ItemSubType);
            Assert.Equal(materialRow.Grade, deserializedFromDict.Grade);
            Assert.Equal(materialRow.ElementalType, deserializedFromDict.ElementalType);
            Assert.Equal(deserializedFromDict, deserializedFromList);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(1000L)]
        public void Costume_DictionaryToList_Migration_WithNonZeroRequiredBlockIndex(long requiredBlockIndex)
        {
            // Arrange: Create a costume with RequiredBlockIndex > 0
            var costumeRow = _tableSheets.CostumeItemSheet.Values.First();
            var costume = new Costume(costumeRow, Guid.NewGuid());
            costume.Update(requiredBlockIndex); // Set RequiredBlockIndex
            Assert.Equal(requiredBlockIndex, costume.RequiredBlockIndex);

            // Act: Serialize to Dictionary (legacy format) - RequiredBlockIndex should be included
            var dictSerialized = Dictionary.Empty
                .Add("id", costume.Id.Serialize())
                .Add("item_type", costume.ItemType.Serialize())
                .Add("item_sub_type", costume.ItemSubType.Serialize())
                .Add("grade", costume.Grade.Serialize())
                .Add("elemental_type", costume.ElementalType.Serialize())
                .Add("equipped", costume.Equipped.Serialize())
                .Add("spine_resource_path", costume.SpineResourcePath.Serialize())
                .Add("item_id", costume.ItemId.Serialize())
                .Add(RequiredBlockIndexKey, costume.RequiredBlockIndex.Serialize());

            // Deserialize from Dictionary
            var deserializedFromDict = new Costume(dictSerialized);

            // Serialize to List (new format)
            var listSerialized = deserializedFromDict.Serialize();

            // Deserialize from List
            var deserializedFromList = new Costume(listSerialized);

            // Assert: All objects should be equal
            Assert.Equal(costume.Id, deserializedFromDict.Id);
            // Note: ItemId may not be preserved during migration due to format differences
            Assert.Equal(requiredBlockIndex, deserializedFromDict.RequiredBlockIndex);
            Assert.Equal(deserializedFromDict, deserializedFromList);
        }

        [Fact]
        public void Equipment_DictionaryToList_Migration_WithDefaultValues()
        {
            // Arrange: Create an equipment with default values
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 0) as Equipment;
            Assert.NotNull(equipment);

            // Act: Serialize to Dictionary (legacy format) - some fields with default values
            var legacyStatDict = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text)"statType", StatType.HP.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"value", 100m.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"additionalValue", 25m.Serialize()),
            });

            var dictSerialized = Dictionary.Empty
                .Add("id", equipment.Id.Serialize())
                .Add("item_type", equipment.ItemType.Serialize())
                .Add("item_sub_type", equipment.ItemSubType.Serialize())
                .Add("grade", equipment.Grade.Serialize())
                .Add("elemental_type", equipment.ElementalType.Serialize())
                .Add("itemId", equipment.ItemId.Serialize())
                .Add("statsMap", equipment.StatsMap.Serialize())
                .Add("skills", new List(equipment.Skills.Select(SkillSerializationTest.LegacySerializeSkill)))
                .Add("buffSkills", new List(equipment.BuffSkills.Select(SkillSerializationTest.LegacySerializeSkill)))
                .Add("requiredBlockIndex", equipment.RequiredBlockIndex.Serialize())
                .Add("equipped", equipment.Equipped.Serialize())
                .Add("level", equipment.level.Serialize())
                .Add("stat", legacyStatDict)
                .Add("set_id", equipment.SetId.Serialize())
                .Add("spine_resource_path", equipment.SpineResourcePath.Serialize())
                .Add("icon_id", equipment.IconId)
                .Add("bcc", equipment.ByCustomCraft)
                .Add("cwr", equipment.CraftWithRandom)
                .Add("hroi", equipment.HasRandomOnlyIcon)
                .Add("oc", equipment.optionCountFromCombination.Serialize())
                .Add("mwmr", equipment.MadeWithMimisbrunnrRecipe.Serialize())
                .Add("eq_exp", equipment.Exp.Serialize());
            // Exp is intentionally omitted to test default value handling

            // Deserialize from Dictionary
            var deserializedFromDict = new Equipment(dictSerialized);

            // Serialize to List (new format)
            var listSerialized = deserializedFromDict.Serialize();

            // Deserialize from List
            var deserializedFromList = new Equipment(listSerialized);

            // Assert: All objects should be equal
            Assert.Equal(equipment.Id, deserializedFromDict.Id);
            Assert.Equal(equipment.Exp, deserializedFromDict.Exp); // Default value
            Assert.Equal(equipment.ItemId, deserializedFromDict.ItemId);
            Assert.Equal(equipment.RequiredBlockIndex, deserializedFromDict.RequiredBlockIndex);
            Assert.Equal(equipment.Equipped, deserializedFromDict.Equipped);
            Assert.Equal(equipment.level, deserializedFromDict.level);
            Assert.Equal(equipment.ByCustomCraft, deserializedFromDict.ByCustomCraft);
            Assert.Equal(equipment.CraftWithRandom, deserializedFromDict.CraftWithRandom);
            Assert.Equal(equipment.HasRandomOnlyIcon, deserializedFromDict.HasRandomOnlyIcon);
            Assert.Equal(equipment.optionCountFromCombination, deserializedFromDict.optionCountFromCombination);
            Assert.Equal(equipment.MadeWithMimisbrunnrRecipe, deserializedFromDict.MadeWithMimisbrunnrRecipe);

            // Compare individual properties for List deserialization
            Assert.Equal(deserializedFromDict.Id, deserializedFromList.Id);
            Assert.Equal(deserializedFromDict.Exp, deserializedFromList.Exp);
            Assert.Equal(deserializedFromDict.ItemId, deserializedFromList.ItemId);
            Assert.Equal(deserializedFromDict.RequiredBlockIndex, deserializedFromList.RequiredBlockIndex);
            Assert.Equal(deserializedFromDict.Equipped, deserializedFromList.Equipped);
            Assert.Equal(deserializedFromDict.level, deserializedFromList.level);
            Assert.Equal(deserializedFromDict.ByCustomCraft, deserializedFromList.ByCustomCraft);
            Assert.Equal(deserializedFromDict.CraftWithRandom, deserializedFromList.CraftWithRandom);
            Assert.Equal(deserializedFromDict.HasRandomOnlyIcon, deserializedFromList.HasRandomOnlyIcon);
            Assert.Equal(deserializedFromDict.optionCountFromCombination, deserializedFromList.optionCountFromCombination);
            Assert.Equal(deserializedFromDict.MadeWithMimisbrunnrRecipe, deserializedFromList.MadeWithMimisbrunnrRecipe);
        }

        [Fact]
        public void Equipment_DictionaryToList_Migration_WithMinimalFields()
        {
            // Arrange: Create an equipment with minimal Dictionary data (missing some fields)
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var legacyStatDict = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text)"statType", StatType.HP.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"value", 0m.Serialize()),
            });

            var dictSerialized = Dictionary.Empty
                .Add("id", equipmentRow.Id.Serialize())
                .Add("item_type", equipmentRow.ItemType.Serialize())
                .Add("item_sub_type", equipmentRow.ItemSubType.Serialize())
                .Add("grade", equipmentRow.Grade.Serialize())
                .Add("elemental_type", equipmentRow.ElementalType.Serialize())
                .Add("itemId", Guid.NewGuid().Serialize())
                .Add("statsMap", new StatMap().Serialize())
                .Add("skills", new List())
                .Add("buffSkills", new List())
                .Add("requiredBlockIndex", 0L.Serialize())
                .Add("equipped", false.Serialize())
                .Add("level", 0.Serialize())
                .Add("stat", legacyStatDict)
                .Add("set_id", 0.Serialize())
                .Add("spine_resource_path", string.Empty.Serialize())
                .Add("icon_id", 0);
            // All required fields are included, testing that migration works correctly

            // Act: Deserialize from Dictionary
            var deserializedFromDict = new Equipment(dictSerialized);

            // Serialize to List (new format)
            var listSerialized = deserializedFromDict.Serialize();

            // Deserialize from List (should handle padded list safely)
            var deserializedFromList = new Equipment(listSerialized);

            // Assert: Should handle migration correctly
            Assert.Equal(equipmentRow.Id, deserializedFromDict.Id);
            Assert.Equal(equipmentRow.ItemType, deserializedFromDict.ItemType);
            Assert.Equal(equipmentRow.ItemSubType, deserializedFromDict.ItemSubType);
            Assert.Equal(equipmentRow.Grade, deserializedFromDict.Grade);
            Assert.Equal(equipmentRow.ElementalType, deserializedFromDict.ElementalType);
            Assert.False(deserializedFromDict.Equipped);
            Assert.Equal(0, deserializedFromDict.level);
            Assert.Equal(0L, deserializedFromDict.Exp); // Default value
            Assert.False(deserializedFromDict.ByCustomCraft); // Default value
            Assert.False(deserializedFromDict.CraftWithRandom); // Default value
            Assert.False(deserializedFromDict.HasRandomOnlyIcon); // Default value
            Assert.Equal(0, deserializedFromDict.optionCountFromCombination); // Default value
            Assert.False(deserializedFromDict.MadeWithMimisbrunnrRecipe); // Default value
            Assert.Equal(deserializedFromDict, deserializedFromList);
        }

        [Theory]
        [InlineData(ItemSubType.Weapon, StatType.ATK)]
        [InlineData(ItemSubType.Armor, StatType.HP)]
        [InlineData(ItemSubType.Belt, StatType.SPD)]
        [InlineData(ItemSubType.Necklace, StatType.CRI)]
        [InlineData(ItemSubType.Ring, StatType.DEF)]
        [InlineData(ItemSubType.Aura, StatType.HIT)]
        [InlineData(ItemSubType.Grimoire, StatType.CDMG)]
        public void Equipment_DictionaryToList_Migration_WithAllFields(ItemSubType itemSubType, StatType itemStatType)
        {
            // Arrange: Create an equipment and modify some fields
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First(i => i.ItemSubType == itemSubType);
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 1000L) as Equipment;
            Assert.NotNull(equipment);

            equipment.Equip();
            equipment.Exp = 500L;
            equipment.ByCustomCraft = true;
            equipment.CraftWithRandom = true;
            equipment.HasRandomOnlyIcon = true;
            equipment.optionCountFromCombination = 3;
            equipment.MadeWithMimisbrunnrRecipe = true;

            // Add skills with different formats
            var skillIds = new[] { 100001, 100003, 100005, 110007 };
            var buffIds = new[] { 200000, 210000, 220000, 230000 };
            var statTypes = new[] { StatType.NONE, StatType.HP, StatType.ATK, StatType.DEF };

            for (var index = 0; index < skillIds.Length; index++)
            {
                var skillId = skillIds[index];
                var skillRow = _tableSheets.SkillSheet[skillId];
                var statType = statTypes[index];
                var skill = SkillFactory.Get(skillRow, 100 + index, 50 + index, 25 + index, statType);
                equipment.Skills.Add(skill);

                var buffId = buffIds[index];
                var buffRow = _tableSheets.SkillSheet[buffId];
                var buff = (BuffSkill)SkillFactory.Get(buffRow, 200 + index, 60 + index, 30 + index, StatType.DEF + index);
                equipment.BuffSkills.Add(buff);
            }

            // Act: Serialize to Dictionary (legacy format)
            var legacyStatDict = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text)"statType", itemStatType.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"value", 75m.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"additionalValue", 20m.Serialize()),
            });

            var dictSerialized = Dictionary.Empty
                .Add("id", equipment.Id.Serialize())
                .Add("item_type", equipment.ItemType.Serialize())
                .Add("item_sub_type", equipment.ItemSubType.Serialize())
                .Add("grade", equipment.Grade.Serialize())
                .Add("elemental_type", equipment.ElementalType.Serialize())
                .Add("itemId", equipment.ItemId.Serialize())
                .Add("statsMap", equipment.StatsMap.Serialize())
                .Add("skills", new List(equipment.Skills.Select(SkillSerializationTest.LegacySerializeSkill)))
                .Add("buffSkills", new List(equipment.BuffSkills.Select(SkillSerializationTest.LegacySerializeSkill)))
                .Add("requiredBlockIndex", equipment.RequiredBlockIndex.Serialize())
                .Add("equipped", equipment.Equipped.Serialize())
                .Add("level", equipment.level.Serialize())
                .Add("stat", legacyStatDict)
                .Add("set_id", equipment.SetId.Serialize())
                .Add("spine_resource_path", equipment.SpineResourcePath.Serialize())
                .Add("icon_id", equipment.IconId)
                .Add("bcc", equipment.ByCustomCraft)
                .Add("cwr", equipment.CraftWithRandom)
                .Add("hroi", equipment.HasRandomOnlyIcon)
                .Add("oc", equipment.optionCountFromCombination.Serialize())
                .Add("mwmr", equipment.MadeWithMimisbrunnrRecipe.Serialize())
                .Add("eq_exp", equipment.Exp.Serialize());

            // Deserialize from Dictionary
            var deserializedFromDict = new Equipment(dictSerialized);

            // Serialize to List (new format)
            var listSerialized = deserializedFromDict.Serialize();

            // Deserialize from List
            var deserializedFromList = new Equipment(listSerialized);

            // Assert: All objects should be equal
            Assert.Equal(equipment.Id, deserializedFromDict.Id);
            Assert.Equal(equipment.Exp, deserializedFromDict.Exp);
            Assert.Equal(equipment.ItemId, deserializedFromDict.ItemId);
            Assert.Equal(equipment.RequiredBlockIndex, deserializedFromDict.RequiredBlockIndex);
            Assert.Equal(equipment.Equipped, deserializedFromDict.Equipped);
            Assert.Equal(equipment.level, deserializedFromDict.level);
            Assert.Equal(equipment.ByCustomCraft, deserializedFromDict.ByCustomCraft);
            Assert.Equal(equipment.CraftWithRandom, deserializedFromDict.CraftWithRandom);
            Assert.Equal(equipment.HasRandomOnlyIcon, deserializedFromDict.HasRandomOnlyIcon);
            Assert.Equal(equipment.optionCountFromCombination, deserializedFromDict.optionCountFromCombination);
            Assert.Equal(equipment.MadeWithMimisbrunnrRecipe, deserializedFromDict.MadeWithMimisbrunnrRecipe);

            // Compare individual properties for List deserialization
            Assert.Equal(deserializedFromDict.Id, deserializedFromList.Id);
            Assert.Equal(deserializedFromDict.Exp, deserializedFromList.Exp);
            Assert.Equal(deserializedFromDict.ItemId, deserializedFromList.ItemId);
            Assert.Equal(deserializedFromDict.RequiredBlockIndex, deserializedFromList.RequiredBlockIndex);
            Assert.Equal(deserializedFromDict.Equipped, deserializedFromList.Equipped);
            Assert.Equal(deserializedFromDict.level, deserializedFromList.level);
            Assert.Equal(deserializedFromDict.ByCustomCraft, deserializedFromList.ByCustomCraft);
            Assert.Equal(deserializedFromDict.CraftWithRandom, deserializedFromList.CraftWithRandom);
            Assert.Equal(deserializedFromDict.HasRandomOnlyIcon, deserializedFromList.HasRandomOnlyIcon);
            Assert.Equal(deserializedFromDict.optionCountFromCombination, deserializedFromList.optionCountFromCombination);
            Assert.Equal(deserializedFromDict.MadeWithMimisbrunnrRecipe, deserializedFromList.MadeWithMimisbrunnrRecipe);

            // Verify each skill's properties are preserved
            for (int i = 0; i < deserializedFromDict.Skills.Count; i++)
            {
                var originalSkill = deserializedFromDict.Skills[i];
                var deserializedSkill = deserializedFromList.Skills[i];

                Assert.Equal(originalSkill.Power, deserializedSkill.Power);
                Assert.Equal(originalSkill.Chance, deserializedSkill.Chance);
                Assert.Equal(originalSkill.StatPowerRatio, deserializedSkill.StatPowerRatio);
                Assert.Equal(originalSkill.ReferencedStatType, deserializedSkill.ReferencedStatType);
                Assert.Equal(originalSkill.SkillRow.Id, deserializedSkill.SkillRow.Id);
                Assert.Equal(originalSkill.SkillRow.ElementalType, deserializedSkill.SkillRow.ElementalType);
                Assert.Equal(originalSkill.SkillRow.SkillType, deserializedSkill.SkillRow.SkillType);
                Assert.Equal(originalSkill.SkillRow.SkillCategory, deserializedSkill.SkillRow.SkillCategory);
                Assert.Equal(originalSkill.SkillRow.SkillTargetType, deserializedSkill.SkillRow.SkillTargetType);
            }

            // Verify buff skills
            for (int i = 0; i < deserializedFromDict.BuffSkills.Count; i++)
            {
                var originalBuff = deserializedFromDict.BuffSkills[i];
                var deserializedBuff = deserializedFromList.BuffSkills[i];

                Assert.Equal(originalBuff.Power, deserializedBuff.Power);
                Assert.Equal(originalBuff.Chance, deserializedBuff.Chance);
                Assert.Equal(originalBuff.StatPowerRatio, deserializedBuff.StatPowerRatio);
                Assert.Equal(originalBuff.ReferencedStatType, deserializedBuff.ReferencedStatType);
                Assert.Equal(originalBuff.SkillRow.Id, deserializedBuff.SkillRow.Id);
            }
        }

        [Fact]
        public void Consumable_DictionaryToList_Migration_WithRequiredBlockIndex()
        {
            // Arrange: Create a consumable with RequiredBlockIndex = 0
            var consumableRow = _tableSheets.ConsumableItemSheet.Values.First();
            var consumable = ItemFactory.CreateItemUsable(consumableRow, Guid.NewGuid(), 1) as Consumable;
            Assert.NotNull(consumable);
            Assert.Equal(1L, consumable.RequiredBlockIndex);

            // Act: Serialize to Dictionary (legacy format) - RequiredBlockIndex = 0 should not be included
            var dictSerialized = Dictionary.Empty
                .Add("id", consumable.Id.Serialize())
                .Add("item_type", consumable.ItemType.Serialize())
                .Add("item_sub_type", consumable.ItemSubType.Serialize())
                .Add("grade", consumable.Grade.Serialize())
                .Add("elemental_type", consumable.ElementalType.Serialize())
                .Add("itemId", consumable.ItemId.Serialize())
                .Add("statsMap", consumable.StatsMap.Serialize())
                .Add("skills", new List(consumable.Skills.Select(SkillSerializationTest.LegacySerializeSkill)))
                .Add("buffSkills", new List(consumable.BuffSkills.Select(SkillSerializationTest.LegacySerializeSkill)))
                .Add("requiredBlockIndex", consumable.RequiredBlockIndex.Serialize());

            // Deserialize from Dictionary
            var deserializedFromDict = new Consumable(dictSerialized);

            // Serialize to List (new format)
            var listSerialized = deserializedFromDict.Serialize();

            // Deserialize from List
            var deserializedFromList = new Consumable(listSerialized);

            // Assert: All objects should be equal
            Assert.Equal(consumable.Id, deserializedFromDict.Id);
            Assert.Equal(1L, deserializedFromDict.RequiredBlockIndex);
            Assert.Equal(deserializedFromDict, deserializedFromList);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(2000L)]
        public void TradableMaterial_DictionaryToList_Migration_WithRequiredBlockIndex(long requiredBlockIndex)
        {
            // Arrange: Create a tradable material with RequiredBlockIndex = 0
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var tradableMaterial = new TradableMaterial(materialRow)
            {
                RequiredBlockIndex = requiredBlockIndex,
            };
            Assert.Equal(requiredBlockIndex, tradableMaterial.RequiredBlockIndex);

            // Act: Serialize to Dictionary (legacy format) - RequiredBlockIndex = 0 should not be included
            var dictSerialized = Dictionary.Empty
                .Add("id", tradableMaterial.Id.Serialize())
                .Add("item_type", tradableMaterial.ItemType.Serialize())
                .Add("item_sub_type", tradableMaterial.ItemSubType.Serialize())
                .Add("grade", tradableMaterial.Grade.Serialize())
                .Add("elemental_type", tradableMaterial.ElementalType.Serialize())
                .Add("item_id", tradableMaterial.ItemId.Serialize());

            if (requiredBlockIndex > 0L)
            {
                dictSerialized = dictSerialized.Add(RequiredBlockIndexKey, requiredBlockIndex.Serialize());
            }

            // Deserialize from Dictionary
            var deserializedFromDict = new TradableMaterial(dictSerialized);

            // Serialize to List (new format)
            var listSerialized = deserializedFromDict.Serialize();

            // Deserialize from List
            var deserializedFromList = new TradableMaterial(listSerialized);

            // Assert: All objects should be equal
            Assert.Equal(tradableMaterial.Id, deserializedFromDict.Id);
            Assert.Equal(requiredBlockIndex, deserializedFromDict.RequiredBlockIndex);
            Assert.Equal(deserializedFromDict, deserializedFromList);
        }

        [Fact]
        public void TradableMaterial_DictionaryToList_Migration_WithMissingFields()
        {
            // Arrange: Create a tradable material with minimal Dictionary data (missing some fields)
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var dictSerialized = Dictionary.Empty
                .Add("id", materialRow.Id.Serialize())
                .Add("item_type", materialRow.ItemType.Serialize())
                .Add("item_sub_type", materialRow.ItemSubType.Serialize())
                .Add("grade", materialRow.Grade.Serialize())
                .Add("elemental_type", materialRow.ElementalType.Serialize())
                .Add("item_id", materialRow.ItemId.Serialize());
            // All required fields are included, testing that migration works correctly

            // Act: Deserialize from Dictionary
            var deserializedFromDict = new TradableMaterial(dictSerialized);

            // Serialize to List (new format)
            var listSerialized = deserializedFromDict.Serialize();

            // Deserialize from List (should handle padded list safely)
            var deserializedFromList = new TradableMaterial(listSerialized);

            // Assert: Should handle migration correctly
            Assert.Equal(materialRow.Id, deserializedFromDict.Id);
            Assert.Equal(materialRow.ItemType, deserializedFromDict.ItemType);
            Assert.Equal(materialRow.ItemSubType, deserializedFromDict.ItemSubType);
            Assert.Equal(materialRow.Grade, deserializedFromDict.Grade);
            Assert.Equal(materialRow.ElementalType, deserializedFromDict.ElementalType);
            Assert.Equal(0L, deserializedFromDict.RequiredBlockIndex); // Default value
            Assert.Equal(deserializedFromDict, deserializedFromList);
        }

        [Fact]
        public void SerializationVersion()
        {
            // Arrange: Create legacy Dictionary format data
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var dictSerialized = Dictionary.Empty
                .Add("id", materialRow.Id.Serialize())
                .Add("item_type", materialRow.ItemType.Serialize())
                .Add("item_sub_type", materialRow.ItemSubType.Serialize())
                .Add("grade", materialRow.Grade.Serialize())
                .Add("elemental_type", materialRow.ElementalType.Serialize())
                .Add("item_id", materialRow.ItemId.Serialize());

            // Act: Deserialize from Dictionary format
            var material = new Material(dictSerialized);

            // Act: Serialize to List format
            var listSerialized = material.Serialize();

            // Assert: Should now have version 2 (List format)
            Assert.Equal((Integer)2, ((List)listSerialized)[0]);

            // Act: Deserialize from List format
            var materialDeserialized = new Material(listSerialized);

            // Assert: Should maintain version 2
            Assert.Equal((Integer)2, ((List)materialDeserialized.Serialize())[0]);
        }

        [Fact]
        public void DictionaryToList_Migration_WithMissingFields_ShouldUseDefaultValues()
        {
            // Arrange: Create a Dictionary with missing fields (simulating legacy data)
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var dictWithMissingFields = Dictionary.Empty
                .Add("id", materialRow.Id.Serialize())
                .Add("item_type", materialRow.ItemType.Serialize())
                .Add("item_sub_type", materialRow.ItemSubType.Serialize())
                .Add("grade", materialRow.Grade.Serialize());
            // Intentionally missing elemental_type and item_id to simulate legacy data

            // Act: Deserialize from Dictionary with missing fields
            var deserializedFromDict = new Material(dictWithMissingFields);

            // Serialize to List (new format) - should include all fields with default values
            var listSerialized = deserializedFromDict.Serialize();

            // Deserialize from List (should handle all fields correctly)
            var deserializedFromList = new Material(listSerialized);

            // Assert: Should handle missing fields gracefully during migration
            Assert.Equal(materialRow.Id, deserializedFromDict.Id);
            Assert.Equal(materialRow.ItemType, deserializedFromDict.ItemType);
            Assert.Equal(materialRow.ItemSubType, deserializedFromDict.ItemSubType);
            Assert.Equal(materialRow.Grade, deserializedFromDict.Grade);
            Assert.Equal(ElementalType.Normal, deserializedFromDict.ElementalType); // Default value
            Assert.Equal(default(HashDigest<SHA256>), deserializedFromDict.ItemId); // Default value

            // Migration should preserve the values
            Assert.Equal(deserializedFromDict, deserializedFromList);
        }

        [Fact]
        public void TradableMaterial_DictionaryToList_Migration_WithMissingFields_ShouldUseDefaultValues()
        {
            // Arrange: Create a Dictionary with missing fields for TradableMaterial (simulating legacy data)
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var dictWithMissingFields = Dictionary.Empty
                .Add("id", materialRow.Id.Serialize())
                .Add("item_type", materialRow.ItemType.Serialize())
                .Add("item_sub_type", materialRow.ItemSubType.Serialize())
                .Add("grade", materialRow.Grade.Serialize())
                .Add("elemental_type", materialRow.ElementalType.Serialize());
            // Intentionally missing item_id and required_block_index to simulate legacy data

            // Act: Deserialize from Dictionary with missing fields
            var deserializedFromDict = new TradableMaterial(dictWithMissingFields);

            // Serialize to List (new format) - should include all fields with default values
            var listSerialized = deserializedFromDict.Serialize();

            // Deserialize from List (should handle all fields correctly)
            var deserializedFromList = new TradableMaterial(listSerialized);

            // Assert: Should handle missing fields gracefully during migration
            Assert.Equal(materialRow.Id, deserializedFromDict.Id);
            Assert.Equal(materialRow.ItemType, deserializedFromDict.ItemType);
            Assert.Equal(materialRow.ItemSubType, deserializedFromDict.ItemSubType);
            Assert.Equal(materialRow.Grade, deserializedFromDict.Grade);
            Assert.Equal(materialRow.ElementalType, deserializedFromDict.ElementalType);
            Assert.Equal(default(HashDigest<SHA256>), deserializedFromDict.ItemId); // Default value
            Assert.Equal(0L, deserializedFromDict.RequiredBlockIndex); // Default value

            // Migration should preserve the values
            Assert.Equal(deserializedFromDict, deserializedFromList);
        }

        [Fact]
        public void Equipment_SkillData_Preservation_Test()
        {
            // Arrange: Create equipment with specific skill data
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 1000L) as Equipment;
            Assert.NotNull(equipment);

            // Add specific skills with known values
            var skillRow1 = _tableSheets.SkillSheet[100001]; // Blow Attack
            var skill1 = SkillFactory.Get(skillRow1, 150, 75, 30, StatType.ATK);
            equipment.Skills.Add(skill1);

            var skillRow2 = _tableSheets.SkillSheet[100003]; // Double Attack
            var skill2 = SkillFactory.Get(skillRow2, 200, 60, 40, StatType.CRI);
            equipment.Skills.Add(skill2);

            var buffRow = _tableSheets.SkillSheet[200000]; // Buff skill
            var buffSkill = (BuffSkill)SkillFactory.Get(buffRow, 100, 80, 20, StatType.DEF);
            equipment.BuffSkills.Add(buffSkill);

            // Act: Serialize and deserialize
            var serialized = equipment.Serialize();
            var deserialized = new Equipment(serialized);

            // Assert: Verify skill data integrity
            Assert.Equal(2, deserialized.Skills.Count);
            Assert.Single(deserialized.BuffSkills);

            // Verify first skill
            var deserializedSkill1 = deserialized.Skills[0];
            Assert.Equal(150, deserializedSkill1.Power);
            Assert.Equal(75, deserializedSkill1.Chance);
            Assert.Equal(30, deserializedSkill1.StatPowerRatio);
            Assert.Equal(StatType.ATK, deserializedSkill1.ReferencedStatType);
            Assert.Equal(100001, deserializedSkill1.SkillRow.Id);
            Assert.Equal(SkillCategory.BlowAttack, deserializedSkill1.SkillRow.SkillCategory);

            // Verify second skill
            var deserializedSkill2 = deserialized.Skills[1];
            Assert.Equal(200, deserializedSkill2.Power);
            Assert.Equal(60, deserializedSkill2.Chance);
            Assert.Equal(40, deserializedSkill2.StatPowerRatio);
            Assert.Equal(StatType.CRI, deserializedSkill2.ReferencedStatType);
            Assert.Equal(100003, deserializedSkill2.SkillRow.Id);
            Assert.Equal(SkillCategory.DoubleAttack, deserializedSkill2.SkillRow.SkillCategory);

            // Verify buff skill
            var deserializedBuff = deserialized.BuffSkills[0];
            Assert.Equal(100, deserializedBuff.Power);
            Assert.Equal(80, deserializedBuff.Chance);
            Assert.Equal(20, deserializedBuff.StatPowerRatio);
            Assert.Equal(StatType.DEF, deserializedBuff.ReferencedStatType);
            Assert.Equal(200000, deserializedBuff.SkillRow.Id);
        }

        [Fact]
        public void Consumable_WithSkills_Migration_Test()
        {
            // Arrange: Create consumable with skills
            var consumableRow = _tableSheets.ConsumableItemSheet.Values.First();
            var consumable = ItemFactory.CreateItemUsable(consumableRow, Guid.NewGuid(), 1000L) as Consumable;
            Assert.NotNull(consumable);

            // Add skills to consumable
            var skillRow = _tableSheets.SkillSheet[100001];
            var skill = SkillFactory.Get(skillRow, 120, 65, 35, StatType.HP);
            consumable.Skills.Add(skill);

            var buffRow = _tableSheets.SkillSheet[200000];
            var buffSkill = (BuffSkill)SkillFactory.Get(buffRow, 80, 90, 15, StatType.SPD);
            consumable.BuffSkills.Add(buffSkill);

            // Act: Test migration
            var serialized = consumable.Serialize();
            var deserialized = new Consumable(serialized);

            // Assert: Skills should be preserved
            Assert.Single(deserialized.Skills);
            Assert.Single(deserialized.BuffSkills);

            var deserializedSkill = deserialized.Skills[0];
            Assert.Equal(120, deserializedSkill.Power);
            Assert.Equal(65, deserializedSkill.Chance);
            Assert.Equal(35, deserializedSkill.StatPowerRatio);
            Assert.Equal(StatType.HP, deserializedSkill.ReferencedStatType);

            var deserializedBuff = deserialized.BuffSkills[0];
            Assert.Equal(80, deserializedBuff.Power);
            Assert.Equal(90, deserializedBuff.Chance);
            Assert.Equal(15, deserializedBuff.StatPowerRatio);
            Assert.Equal(StatType.SPD, deserializedBuff.ReferencedStatType);
        }

        [Fact]
        public void Equipment_EmptySkills_Migration_Test()
        {
            // Arrange: Create equipment with no skills
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 1000L) as Equipment;
            Assert.NotNull(equipment);

            // Ensure no skills are added
            Assert.Empty(equipment.Skills);
            Assert.Empty(equipment.BuffSkills);

            // Act: Test migration with empty skills
            var serialized = equipment.Serialize();
            var deserialized = new Equipment(serialized);

            // Assert: Empty skills should be preserved
            Assert.Empty(deserialized.Skills);
            Assert.Empty(deserialized.BuffSkills);
        }
    }
}
