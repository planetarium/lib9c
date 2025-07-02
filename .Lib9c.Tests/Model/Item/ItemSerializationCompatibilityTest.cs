namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    /// <summary>
    /// Tests for ItemBase and its derived classes serialization/deserialization compatibility.
    /// Ensures that both Dictionary (legacy) and List (new) formats are supported.
    /// </summary>
    public class ItemSerializationCompatibilityTest
    {
        private readonly TableSheets _tableSheets;

        public ItemSerializationCompatibilityTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Material_DictionaryToDictionary_Compatibility()
        {
            // Arrange
            var materialRow = _tableSheets.MaterialItemSheet.First;
            var originalMaterial = new Material(materialRow);

            // Act - Serialize to Dictionary (legacy format)
            var serialized = originalMaterial.Serialize();
            var deserialized = new Material(serialized);

            // Assert
            Assert.Equal(originalMaterial.Id, deserialized.Id);
            Assert.Equal(originalMaterial.Grade, deserialized.Grade);
            Assert.Equal(originalMaterial.ItemType, deserialized.ItemType);
            Assert.Equal(originalMaterial.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalMaterial.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalMaterial.ItemId, deserialized.ItemId);
        }

        [Fact]
        public void Material_ListToDictionary_Compatibility()
        {
            // Arrange
            var materialRow = _tableSheets.MaterialItemSheet.First;
            var originalMaterial = new Material(materialRow);

            // Act - Serialize to List (new format)
            var serialized = originalMaterial.Serialize();
            var deserialized = new Material(serialized);

            // Assert
            Assert.Equal(originalMaterial.Id, deserialized.Id);
            Assert.Equal(originalMaterial.Grade, deserialized.Grade);
            Assert.Equal(originalMaterial.ItemType, deserialized.ItemType);
            Assert.Equal(originalMaterial.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalMaterial.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalMaterial.ItemId, deserialized.ItemId);
        }

        [Fact]
        public void Consumable_DictionaryToDictionary_Compatibility()
        {
            // Arrange
            var consumableRow = _tableSheets.ConsumableItemSheet.First;
            var originalConsumable = new Consumable(consumableRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to Dictionary (legacy format)
            var serialized = originalConsumable.Serialize();
            var deserialized = new Consumable(serialized);

            // Assert
            Assert.Equal(originalConsumable.Id, deserialized.Id);
            Assert.Equal(originalConsumable.Grade, deserialized.Grade);
            Assert.Equal(originalConsumable.ItemType, deserialized.ItemType);
            Assert.Equal(originalConsumable.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalConsumable.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalConsumable.ItemId, deserialized.ItemId);
            Assert.Equal(originalConsumable.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalConsumable.Stats.Count, deserialized.Stats.Count);
        }

        [Fact]
        public void Consumable_ListToDictionary_Compatibility()
        {
            // Arrange
            var consumableRow = _tableSheets.ConsumableItemSheet.First;
            var originalConsumable = new Consumable(consumableRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to List (new format)
            var serialized = originalConsumable.Serialize();
            var deserialized = new Consumable(serialized);

            // Assert
            Assert.Equal(originalConsumable.Id, deserialized.Id);
            Assert.Equal(originalConsumable.Grade, deserialized.Grade);
            Assert.Equal(originalConsumable.ItemType, deserialized.ItemType);
            Assert.Equal(originalConsumable.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalConsumable.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalConsumable.ItemId, deserialized.ItemId);
            Assert.Equal(originalConsumable.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalConsumable.Stats.Count, deserialized.Stats.Count);
        }

        [Fact]
        public void Costume_DictionaryToDictionary_Compatibility()
        {
            // Arrange
            var costumeRow = _tableSheets.CostumeItemSheet.First;
            var originalCostume = new Costume(costumeRow, Guid.NewGuid());

            // Act - Serialize to Dictionary (legacy format)
            var serialized = originalCostume.Serialize();
            var deserialized = new Costume(serialized);

            // Assert
            Assert.Equal(originalCostume.Id, deserialized.Id);
            Assert.Equal(originalCostume.Grade, deserialized.Grade);
            Assert.Equal(originalCostume.ItemType, deserialized.ItemType);
            Assert.Equal(originalCostume.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalCostume.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalCostume.ItemId, deserialized.ItemId);
            Assert.Equal(originalCostume.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalCostume.Equipped, deserialized.Equipped);
        }

        [Fact]
        public void Costume_ListToDictionary_Compatibility()
        {
            // Arrange
            var costumeRow = _tableSheets.CostumeItemSheet.First;
            var originalCostume = new Costume(costumeRow, Guid.NewGuid());

            // Act - Serialize to List (new format)
            var serialized = originalCostume.Serialize();
            var deserialized = new Costume(serialized);

            // Assert
            Assert.Equal(originalCostume.Id, deserialized.Id);
            Assert.Equal(originalCostume.Grade, deserialized.Grade);
            Assert.Equal(originalCostume.ItemType, deserialized.ItemType);
            Assert.Equal(originalCostume.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalCostume.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalCostume.ItemId, deserialized.ItemId);
            Assert.Equal(originalCostume.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalCostume.Equipped, deserialized.Equipped);
        }

        [Fact]
        public void Equipment_DictionaryToDictionary_Compatibility()
        {
            // Arrange
            var equipmentRow = _tableSheets.EquipmentItemSheet.First;
            var originalEquipment = new Equipment(equipmentRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to Dictionary (legacy format)
            var serialized = originalEquipment.Serialize();
            var deserialized = new Equipment(serialized);

            // Assert
            Assert.Equal(originalEquipment.Id, deserialized.Id);
            Assert.Equal(originalEquipment.Grade, deserialized.Grade);
            Assert.Equal(originalEquipment.ItemType, deserialized.ItemType);
            Assert.Equal(originalEquipment.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalEquipment.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalEquipment.ItemId, deserialized.ItemId);
            Assert.Equal(originalEquipment.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalEquipment.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalEquipment.SetId, deserialized.SetId);
            Assert.Equal(originalEquipment.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalEquipment.Equipped, deserialized.Equipped);
            Assert.Equal(originalEquipment.level, deserialized.level);
            Assert.Equal(originalEquipment.Exp, deserialized.Exp);
        }

        [Fact]
        public void Equipment_ListToDictionary_Compatibility()
        {
            // Arrange
            var equipmentRow = _tableSheets.EquipmentItemSheet.First;
            var originalEquipment = new Equipment(equipmentRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to List (new format)
            var serialized = originalEquipment.Serialize();
            var deserialized = new Equipment(serialized);

            // Assert
            Assert.Equal(originalEquipment.Id, deserialized.Id);
            Assert.Equal(originalEquipment.Grade, deserialized.Grade);
            Assert.Equal(originalEquipment.ItemType, deserialized.ItemType);
            Assert.Equal(originalEquipment.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalEquipment.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalEquipment.ItemId, deserialized.ItemId);
            Assert.Equal(originalEquipment.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalEquipment.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalEquipment.SetId, deserialized.SetId);
            Assert.Equal(originalEquipment.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalEquipment.Equipped, deserialized.Equipped);
            Assert.Equal(originalEquipment.level, deserialized.level);
            Assert.Equal(originalEquipment.Exp, deserialized.Exp);
        }

        [Fact]
        public void Weapon_DictionaryToDictionary_Compatibility()
        {
            // Arrange
            var weaponRow = _tableSheets.EquipmentItemSheet.First;
            var originalWeapon = new Weapon(weaponRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to Dictionary (legacy format)
            var serialized = originalWeapon.Serialize();
            var deserialized = new Weapon(serialized);

            // Assert
            Assert.Equal(originalWeapon.Id, deserialized.Id);
            Assert.Equal(originalWeapon.Grade, deserialized.Grade);
            Assert.Equal(originalWeapon.ItemType, deserialized.ItemType);
            Assert.Equal(originalWeapon.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalWeapon.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalWeapon.ItemId, deserialized.ItemId);
            Assert.Equal(originalWeapon.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalWeapon.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalWeapon.SetId, deserialized.SetId);
            Assert.Equal(originalWeapon.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalWeapon.Equipped, deserialized.Equipped);
            Assert.Equal(originalWeapon.level, deserialized.level);
            Assert.Equal(originalWeapon.Exp, deserialized.Exp);
        }

        [Fact]
        public void Weapon_ListToDictionary_Compatibility()
        {
            // Arrange
            var weaponRow = _tableSheets.EquipmentItemSheet.First;
            var originalWeapon = new Weapon(weaponRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to List (new format)
            var serialized = originalWeapon.Serialize();
            var deserialized = new Weapon(serialized);

            // Assert
            Assert.Equal(originalWeapon.Id, deserialized.Id);
            Assert.Equal(originalWeapon.Grade, deserialized.Grade);
            Assert.Equal(originalWeapon.ItemType, deserialized.ItemType);
            Assert.Equal(originalWeapon.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalWeapon.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalWeapon.ItemId, deserialized.ItemId);
            Assert.Equal(originalWeapon.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalWeapon.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalWeapon.SetId, deserialized.SetId);
            Assert.Equal(originalWeapon.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalWeapon.Equipped, deserialized.Equipped);
            Assert.Equal(originalWeapon.level, deserialized.level);
            Assert.Equal(originalWeapon.Exp, deserialized.Exp);
        }

        [Fact]
        public void Armor_DictionaryToDictionary_Compatibility()
        {
            // Arrange
            var armorRow = _tableSheets.EquipmentItemSheet.First;
            var originalArmor = new Armor(armorRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to Dictionary (legacy format)
            var serialized = originalArmor.Serialize();
            var deserialized = new Armor(serialized);

            // Assert
            Assert.Equal(originalArmor.Id, deserialized.Id);
            Assert.Equal(originalArmor.Grade, deserialized.Grade);
            Assert.Equal(originalArmor.ItemType, deserialized.ItemType);
            Assert.Equal(originalArmor.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalArmor.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalArmor.ItemId, deserialized.ItemId);
            Assert.Equal(originalArmor.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalArmor.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalArmor.SetId, deserialized.SetId);
            Assert.Equal(originalArmor.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalArmor.Equipped, deserialized.Equipped);
            Assert.Equal(originalArmor.level, deserialized.level);
            Assert.Equal(originalArmor.Exp, deserialized.Exp);
        }

        [Fact]
        public void Armor_ListToDictionary_Compatibility()
        {
            // Arrange
            var armorRow = _tableSheets.EquipmentItemSheet.First;
            var originalArmor = new Armor(armorRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to List (new format)
            var serialized = originalArmor.Serialize();
            var deserialized = new Armor(serialized);

            // Assert
            Assert.Equal(originalArmor.Id, deserialized.Id);
            Assert.Equal(originalArmor.Grade, deserialized.Grade);
            Assert.Equal(originalArmor.ItemType, deserialized.ItemType);
            Assert.Equal(originalArmor.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalArmor.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalArmor.ItemId, deserialized.ItemId);
            Assert.Equal(originalArmor.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalArmor.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalArmor.SetId, deserialized.SetId);
            Assert.Equal(originalArmor.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalArmor.Equipped, deserialized.Equipped);
            Assert.Equal(originalArmor.level, deserialized.level);
            Assert.Equal(originalArmor.Exp, deserialized.Exp);
        }

        [Fact]
        public void Belt_DictionaryToDictionary_Compatibility()
        {
            // Arrange
            var beltRow = _tableSheets.EquipmentItemSheet.First;
            var originalBelt = new Belt(beltRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to Dictionary (legacy format)
            var serialized = originalBelt.Serialize();
            var deserialized = new Belt(serialized);

            // Assert
            Assert.Equal(originalBelt.Id, deserialized.Id);
            Assert.Equal(originalBelt.Grade, deserialized.Grade);
            Assert.Equal(originalBelt.ItemType, deserialized.ItemType);
            Assert.Equal(originalBelt.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalBelt.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalBelt.ItemId, deserialized.ItemId);
            Assert.Equal(originalBelt.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalBelt.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalBelt.SetId, deserialized.SetId);
            Assert.Equal(originalBelt.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalBelt.Equipped, deserialized.Equipped);
            Assert.Equal(originalBelt.level, deserialized.level);
            Assert.Equal(originalBelt.Exp, deserialized.Exp);
        }

        [Fact]
        public void Belt_ListToDictionary_Compatibility()
        {
            // Arrange
            var beltRow = _tableSheets.EquipmentItemSheet.First;
            var originalBelt = new Belt(beltRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to List (new format)
            var serialized = originalBelt.Serialize();
            var deserialized = new Belt(serialized);

            // Assert
            Assert.Equal(originalBelt.Id, deserialized.Id);
            Assert.Equal(originalBelt.Grade, deserialized.Grade);
            Assert.Equal(originalBelt.ItemType, deserialized.ItemType);
            Assert.Equal(originalBelt.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalBelt.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalBelt.ItemId, deserialized.ItemId);
            Assert.Equal(originalBelt.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalBelt.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalBelt.SetId, deserialized.SetId);
            Assert.Equal(originalBelt.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalBelt.Equipped, deserialized.Equipped);
            Assert.Equal(originalBelt.level, deserialized.level);
            Assert.Equal(originalBelt.Exp, deserialized.Exp);
        }

        [Fact]
        public void Necklace_DictionaryToDictionary_Compatibility()
        {
            // Arrange
            var necklaceRow = _tableSheets.EquipmentItemSheet.First;
            var originalNecklace = new Necklace(necklaceRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to Dictionary (legacy format)
            var serialized = originalNecklace.Serialize();
            var deserialized = new Necklace(serialized);

            // Assert
            Assert.Equal(originalNecklace.Id, deserialized.Id);
            Assert.Equal(originalNecklace.Grade, deserialized.Grade);
            Assert.Equal(originalNecklace.ItemType, deserialized.ItemType);
            Assert.Equal(originalNecklace.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalNecklace.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalNecklace.ItemId, deserialized.ItemId);
            Assert.Equal(originalNecklace.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalNecklace.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalNecklace.SetId, deserialized.SetId);
            Assert.Equal(originalNecklace.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalNecklace.Equipped, deserialized.Equipped);
            Assert.Equal(originalNecklace.level, deserialized.level);
            Assert.Equal(originalNecklace.Exp, deserialized.Exp);
        }

        [Fact]
        public void Necklace_ListToDictionary_Compatibility()
        {
            // Arrange
            var necklaceRow = _tableSheets.EquipmentItemSheet.First;
            var originalNecklace = new Necklace(necklaceRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to List (new format)
            var serialized = originalNecklace.Serialize();
            var deserialized = new Necklace(serialized);

            // Assert
            Assert.Equal(originalNecklace.Id, deserialized.Id);
            Assert.Equal(originalNecklace.Grade, deserialized.Grade);
            Assert.Equal(originalNecklace.ItemType, deserialized.ItemType);
            Assert.Equal(originalNecklace.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalNecklace.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalNecklace.ItemId, deserialized.ItemId);
            Assert.Equal(originalNecklace.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalNecklace.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalNecklace.SetId, deserialized.SetId);
            Assert.Equal(originalNecklace.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalNecklace.Equipped, deserialized.Equipped);
            Assert.Equal(originalNecklace.level, deserialized.level);
            Assert.Equal(originalNecklace.Exp, deserialized.Exp);
        }

        [Fact]
        public void Ring_DictionaryToDictionary_Compatibility()
        {
            // Arrange
            var ringRow = _tableSheets.EquipmentItemSheet.First;
            var originalRing = new Ring(ringRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to Dictionary (legacy format)
            var serialized = originalRing.Serialize();
            var deserialized = new Ring(serialized);

            // Assert
            Assert.Equal(originalRing.Id, deserialized.Id);
            Assert.Equal(originalRing.Grade, deserialized.Grade);
            Assert.Equal(originalRing.ItemType, deserialized.ItemType);
            Assert.Equal(originalRing.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalRing.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalRing.ItemId, deserialized.ItemId);
            Assert.Equal(originalRing.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalRing.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalRing.SetId, deserialized.SetId);
            Assert.Equal(originalRing.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalRing.Equipped, deserialized.Equipped);
            Assert.Equal(originalRing.level, deserialized.level);
            Assert.Equal(originalRing.Exp, deserialized.Exp);
        }

        [Fact]
        public void Ring_ListToDictionary_Compatibility()
        {
            // Arrange
            var ringRow = _tableSheets.EquipmentItemSheet.First;
            var originalRing = new Ring(ringRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to List (new format)
            var serialized = originalRing.Serialize();
            var deserialized = new Ring(serialized);

            // Assert
            Assert.Equal(originalRing.Id, deserialized.Id);
            Assert.Equal(originalRing.Grade, deserialized.Grade);
            Assert.Equal(originalRing.ItemType, deserialized.ItemType);
            Assert.Equal(originalRing.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalRing.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalRing.ItemId, deserialized.ItemId);
            Assert.Equal(originalRing.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalRing.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalRing.SetId, deserialized.SetId);
            Assert.Equal(originalRing.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalRing.Equipped, deserialized.Equipped);
            Assert.Equal(originalRing.level, deserialized.level);
            Assert.Equal(originalRing.Exp, deserialized.Exp);
        }

        [Fact]
        public void Aura_DictionaryToDictionary_Compatibility()
        {
            // Arrange
            var auraRow = _tableSheets.EquipmentItemSheet.First;
            var originalAura = new Aura(auraRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to Dictionary (legacy format)
            var serialized = originalAura.Serialize();
            var deserialized = new Aura(serialized);

            // Assert
            Assert.Equal(originalAura.Id, deserialized.Id);
            Assert.Equal(originalAura.Grade, deserialized.Grade);
            Assert.Equal(originalAura.ItemType, deserialized.ItemType);
            Assert.Equal(originalAura.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalAura.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalAura.ItemId, deserialized.ItemId);
            Assert.Equal(originalAura.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalAura.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalAura.SetId, deserialized.SetId);
            Assert.Equal(originalAura.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalAura.Equipped, deserialized.Equipped);
            Assert.Equal(originalAura.level, deserialized.level);
            Assert.Equal(originalAura.Exp, deserialized.Exp);
        }

        [Fact]
        public void Aura_ListToDictionary_Compatibility()
        {
            // Arrange
            var auraRow = _tableSheets.EquipmentItemSheet.First;
            var originalAura = new Aura(auraRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to List (new format)
            var serialized = originalAura.Serialize();
            var deserialized = new Aura(serialized);

            // Assert
            Assert.Equal(originalAura.Id, deserialized.Id);
            Assert.Equal(originalAura.Grade, deserialized.Grade);
            Assert.Equal(originalAura.ItemType, deserialized.ItemType);
            Assert.Equal(originalAura.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalAura.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalAura.ItemId, deserialized.ItemId);
            Assert.Equal(originalAura.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalAura.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalAura.SetId, deserialized.SetId);
            Assert.Equal(originalAura.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalAura.Equipped, deserialized.Equipped);
            Assert.Equal(originalAura.level, deserialized.level);
            Assert.Equal(originalAura.Exp, deserialized.Exp);
        }

        [Fact]
        public void Grimoire_DictionaryToDictionary_Compatibility()
        {
            // Arrange
            var grimoireRow = _tableSheets.EquipmentItemSheet.First;
            var originalGrimoire = new Grimoire(grimoireRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to Dictionary (legacy format)
            var serialized = originalGrimoire.Serialize();
            var deserialized = new Grimoire(serialized);

            // Assert
            Assert.Equal(originalGrimoire.Id, deserialized.Id);
            Assert.Equal(originalGrimoire.Grade, deserialized.Grade);
            Assert.Equal(originalGrimoire.ItemType, deserialized.ItemType);
            Assert.Equal(originalGrimoire.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalGrimoire.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalGrimoire.ItemId, deserialized.ItemId);
            Assert.Equal(originalGrimoire.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalGrimoire.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalGrimoire.SetId, deserialized.SetId);
            Assert.Equal(originalGrimoire.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalGrimoire.Equipped, deserialized.Equipped);
            Assert.Equal(originalGrimoire.level, deserialized.level);
            Assert.Equal(originalGrimoire.Exp, deserialized.Exp);
        }

        [Fact]
        public void Grimoire_ListToDictionary_Compatibility()
        {
            // Arrange
            var grimoireRow = _tableSheets.EquipmentItemSheet.First;
            var originalGrimoire = new Grimoire(grimoireRow, Guid.NewGuid(), 1000L);

            // Act - Serialize to List (new format)
            var serialized = originalGrimoire.Serialize();
            var deserialized = new Grimoire(serialized);

            // Assert
            Assert.Equal(originalGrimoire.Id, deserialized.Id);
            Assert.Equal(originalGrimoire.Grade, deserialized.Grade);
            Assert.Equal(originalGrimoire.ItemType, deserialized.ItemType);
            Assert.Equal(originalGrimoire.ItemSubType, deserialized.ItemSubType);
            Assert.Equal(originalGrimoire.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalGrimoire.ItemId, deserialized.ItemId);
            Assert.Equal(originalGrimoire.RequiredBlockIndex, deserialized.RequiredBlockIndex);
            Assert.Equal(originalGrimoire.Stat.StatType, deserialized.Stat.StatType);
            Assert.Equal(originalGrimoire.SetId, deserialized.SetId);
            Assert.Equal(originalGrimoire.SpineResourcePath, deserialized.SpineResourcePath);
            Assert.Equal(originalGrimoire.Equipped, deserialized.Equipped);
            Assert.Equal(originalGrimoire.level, deserialized.level);
            Assert.Equal(originalGrimoire.Exp, deserialized.Exp);
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
        }

        [Fact]
        public void Deserialize_SupportsBothFormats()
        {
            // Arrange
            var materialRow = _tableSheets.MaterialItemSheet.First;
            var originalMaterial = new Material(materialRow);

            // Act & Assert - List format
            var listSerialized = originalMaterial.Serialize();
            var listDeserialized = new Material(listSerialized);
            Assert.Equal(originalMaterial.Id, listDeserialized.Id);
            Assert.Equal(originalMaterial.ItemId, listDeserialized.ItemId);

            // Act & Assert - Dictionary format (legacy)
            var dictSerialized = Dictionary.Empty
                .Add("id", originalMaterial.Id.Serialize())
                .Add("item_type", originalMaterial.ItemType.Serialize())
                .Add("item_sub_type", originalMaterial.ItemSubType.Serialize())
                .Add("grade", originalMaterial.Grade.Serialize())
                .Add("elemental_type", originalMaterial.ElementalType.Serialize())
                .Add("item_id", originalMaterial.ItemId.Serialize());
            var dictDeserialized = new Material(dictSerialized);
            Assert.Equal(originalMaterial.Id, dictDeserialized.Id);
            Assert.Equal(originalMaterial.ItemId, dictDeserialized.ItemId);
        }
    }
}
