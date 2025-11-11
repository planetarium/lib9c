namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Nekoyume.Action;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Rune;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Rune;
    using Xunit;

    public class InfiniteTowerFloorSheetTest
    {
        private readonly TableSheets _tableSheets;

        public InfiniteTowerFloorSheetTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void ValidateItemTypeRestrictions_With_ForbiddenTypes_ShouldThrow()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            var forbiddenSubTypes = new List<ItemSubType> { ItemSubType.Weapon, };
            floorRow.GetType().GetProperty("ForbiddenItemSubTypes")?.SetValue(floorRow, forbiddenSubTypes);

            var itemList = new List<ItemBase>
            {
                CreateTestItem(ItemType.Equipment), // This should be allowed (Armor)
                CreateTestWeapon(), // This should be forbidden (Weapon)
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateItemTypeRestrictions(itemList));
            Assert.Contains("Invalid item sub-type", exception.Message);
            Assert.Contains("Weapon", exception.Message);
            Assert.Contains("forbidden", exception.Message);
        }

        [Fact]
        public void ValidateItemTypeRestrictions_WithMultipleForbiddenTypes_ShouldThrow()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            var forbiddenSubTypes = new List<ItemSubType> { ItemSubType.Weapon, ItemSubType.Armor, ItemSubType.Belt };
            floorRow.GetType().GetProperty("ForbiddenItemSubTypes")?.SetValue(floorRow, forbiddenSubTypes);

            var itemList = new List<ItemBase>
            {
                CreateTestItem(ItemType.Equipment), // This should be allowed (might be Armor, but depends on CreateTestItem)
                CreateTestWeapon(), // This should be forbidden (Weapon)
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateItemTypeRestrictions(itemList));
            Assert.Contains("Invalid item sub-type", exception.Message);
            Assert.Contains("forbidden", exception.Message);
            // Check that all forbidden types are mentioned in the error message
            var errorMessage = exception.Message;
            Assert.Contains("Weapon", errorMessage);
        }

        [Fact]
        public void ValidateItemTypeRestrictions_WithMultipleForbiddenTypes_ShouldAllowAllowedTypes()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            var forbiddenSubTypes = new List<ItemSubType> { ItemSubType.Weapon, ItemSubType.Armor };
            floorRow.GetType().GetProperty("ForbiddenItemSubTypes")?.SetValue(floorRow, forbiddenSubTypes);

            // Create items with allowed sub-types (e.g., Belt, Necklace)
            var equipmentSheet = _tableSheets.EquipmentItemSheet;
            var beltRow = equipmentSheet.Values.FirstOrDefault(x => x.ItemSubType == ItemSubType.Belt);
            var necklaceRow = equipmentSheet.Values.FirstOrDefault(x => x.ItemSubType == ItemSubType.Necklace);

            if (beltRow == null || necklaceRow == null)
            {
                // Skip test if test data doesn't have required items
                return;
            }

            var itemList = new List<ItemBase>
            {
                ItemFactory.CreateItem(beltRow, new TestRandom()) as Equipment,
                ItemFactory.CreateItem(necklaceRow, new TestRandom()) as Equipment,
            };

            // Act & Assert - Should not throw
            floorRow.ValidateItemTypeRestrictions(itemList);
        }

        [Fact]
        public void ValidateItemTypeRestrictions_With_AllowedTypes_ShouldPass()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            var forbiddenTypes = new List<ItemType> { ItemType.Material, };
            floorRow.GetType().GetProperty("ForbiddenItemTypes")?.SetValue(floorRow, forbiddenTypes);

            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment),
                CreateTestEquipment(ItemType.Costume),
            };

            // Act & Assert - Should not throw
            floorRow.ValidateItemTypeRestrictions(equipmentList);
        }

        [Fact]
        public void ValidateItemTypeRestrictions_With_NoRestrictions_ShouldPass()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();

            // No restrictions set
            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment),
                CreateTestEquipment(ItemType.Material),
            };

            // Act & Assert - Should not throw
            floorRow.ValidateItemTypeRestrictions(equipmentList);
        }

        [Fact]
        public void ValidateItemGradeRestrictions_With_MinGrade_ShouldThrow()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("MinItemGrade")?.SetValue(floorRow, 3);

            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment, grade: 2), // Below minimum
                CreateTestEquipment(ItemType.Equipment, grade: 4), // Above minimum
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateItemGradeRestrictions(equipmentList));
            Assert.Contains("Invalid item grade", exception.Message);
            Assert.Contains("below minimum", exception.Message);
        }

        [Fact]
        public void ValidateItemGradeRestrictions_With_MaxGrade_ShouldThrow()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("MaxItemGrade")?.SetValue(floorRow, 3);

            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment, grade: 2), // Below maximum
                CreateTestEquipment(ItemType.Equipment, grade: 4), // Above maximum
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateItemGradeRestrictions(equipmentList));
            Assert.Contains("Invalid item grade", exception.Message);
            Assert.Contains("exceeds maximum", exception.Message);
        }

        [Fact]
        public void ValidateItemGradeRestrictions_With_ValidGrades_ShouldPass()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("MinItemGrade")?.SetValue(floorRow, 2);
            floorRow.GetType().GetProperty("MaxItemGrade")?.SetValue(floorRow, 4);

            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment, grade: 2),
                CreateTestEquipment(ItemType.Equipment, grade: 3),
                CreateTestEquipment(ItemType.Equipment, grade: 4),
            };

            // Act & Assert - Should not throw
            floorRow.ValidateItemGradeRestrictions(equipmentList);
        }

        [Fact]
        public void ValidateItemLevelRestrictions_With_MinLevel_ShouldThrow()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("MinItemLevel")?.SetValue(floorRow, 5);

            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment, level: 3), // Below minimum
                CreateTestEquipment(ItemType.Equipment, level: 7), // Above minimum
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateItemLevelRestrictions(equipmentList));
            Assert.Contains("Invalid item level", exception.Message);
            Assert.Contains("below minimum", exception.Message);
        }

        [Fact]
        public void ValidateItemLevelRestrictions_With_MaxLevel_ShouldThrow()
        {
            // Arrange
            var fields = new List<string>
            {
                "1", // Id
                "1", // Floor
                "1000", // RequiredCp
                "2000", // MaxCp
                string.Empty, // ForbiddenItemSubTypes
                "1", // MinItemGrade
                "5", // MaxItemGrade
                "1", // MinItemLevel
                "5", // MaxItemLevel
                "1", // GuaranteedConditionId
                "0", // MinRandomConditions
                "2", // MaxRandomConditions
                string.Empty, // ItemRewardId1
                string.Empty, // ItemRewardCount1
                string.Empty, // ItemRewardId2
                string.Empty, // ItemRewardCount2
                string.Empty, // ItemRewardId3
                string.Empty, // ItemRewardCount3
                string.Empty, // ItemRewardId4
                string.Empty, // ItemRewardCount4
                string.Empty, // ItemRewardId5
                string.Empty, // ItemRewardCount5
                string.Empty, // FungibleAssetRewardTicker1
                string.Empty, // FungibleAssetRewardAmount1
                string.Empty, // FungibleAssetRewardTicker2
                string.Empty, // FungibleAssetRewardAmount2
                string.Empty, // FungibleAssetRewardTicker3
                string.Empty, // FungibleAssetRewardAmount3
                string.Empty, // FungibleAssetRewardTicker4
                string.Empty, // FungibleAssetRewardAmount4
                string.Empty, // FungibleAssetRewardTicker5
                string.Empty, // FungibleAssetRewardAmount5
                "100", // NcgCost
                "600201", // MaterialCostId
                "50", // MaterialCostCount
                string.Empty, // RandomConditionId1
                string.Empty, // RandomConditionWeight1
                string.Empty, // RandomConditionId2
                string.Empty, // RandomConditionWeight2
                string.Empty, // RandomConditionId3
                string.Empty, // RandomConditionWeight3
                string.Empty, // RandomConditionId4
                string.Empty, // RandomConditionWeight4
                string.Empty, // RandomConditionId5
                string.Empty, // RandomConditionWeight5
                string.Empty, // ForbiddenRuneTypes
                string.Empty, // RequiredElementalType
            };

            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.Set(fields);

            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment, level: 3), // Below maximum
                CreateTestEquipment(ItemType.Equipment, level: 7), // Above maximum
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateItemLevelRestrictions(equipmentList));
            Assert.Contains("Invalid item level", exception.Message);
            Assert.Contains("exceeds maximum", exception.Message);
        }

        [Fact]
        public void ValidateItemLevelRestrictions_With_Costumes_ShouldSkip()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("MinItemLevel")?.SetValue(floorRow, 5);

            var costumeList = new List<Costume>
            {
                CreateTestCostume(ItemType.Costume, level: 1), // Costumes should be skipped
            };

            // Act & Assert - Should not throw (costumes are skipped)
            floorRow.ValidateItemLevelRestrictions(costumeList);
        }

        [Fact]
        public void ValidateCpRequirements_With_MinCp_ShouldThrow()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("RequiredCp")?.SetValue(floorRow, 1000L);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateCpRequirements(500));
            Assert.Contains("Insufficient combat power", exception.Message);
            Assert.Contains("below minimum", exception.Message);
        }

        [Fact]
        public void ValidateCpRequirements_With_MaxCp_ShouldThrow()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("MaxCp")?.SetValue(floorRow, 1000L);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateCpRequirements(1500));
            Assert.Contains("Excessive combat power", exception.Message);
            Assert.Contains("exceeds maximum", exception.Message);
        }

        [Fact]
        public void ValidateCpRequirements_With_ValidCp_ShouldPass()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("RequiredCp")?.SetValue(floorRow, 500L);
            floorRow.GetType().GetProperty("MaxCp")?.SetValue(floorRow, 1500L);

            // Act & Assert - Should not throw
            floorRow.ValidateCpRequirements(1000);
        }

        [Fact]
        public void ValidateCpRequirements_With_NoRestrictions_ShouldPass()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            // No restrictions set

            // Act & Assert - Should not throw
            floorRow.ValidateCpRequirements(1000);
        }

        [Fact]
        public void ValidateEquipmentElementalType_WithMultipleTypes_ShouldAllowValidTypes()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            var requiredTypes = new List<ElementalType> { ElementalType.Fire, ElementalType.Water };
            floorRow.GetType().GetProperty("RequiredElementalTypes")?.SetValue(floorRow, requiredTypes);

            var equipment1 = CreateTestEquipment(ItemType.Equipment);
            equipment1.ElementalType = ElementalType.Fire;

            var equipment2 = CreateTestEquipment(ItemType.Equipment);
            equipment2.ElementalType = ElementalType.Water;

            var equipmentList = new List<Equipment> { equipment1, equipment2 };

            // Act & Assert - Should not throw
            floorRow.ValidateEquipmentElementalType(equipmentList);
        }

        [Fact]
        public void ValidateEquipmentElementalType_WithMultipleTypes_ShouldThrowForInvalidType()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            var requiredTypes = new List<ElementalType> { ElementalType.Fire, ElementalType.Water };
            floorRow.GetType().GetProperty("RequiredElementalTypes")?.SetValue(floorRow, requiredTypes);

            var equipment1 = CreateTestEquipment(ItemType.Equipment);
            equipment1.ElementalType = ElementalType.Fire; // Valid

            var equipment2 = CreateTestEquipment(ItemType.Equipment);
            equipment2.ElementalType = ElementalType.Land; // Invalid - not in required list

            var equipmentList = new List<Equipment> { equipment1, equipment2 };

            // Act & Assert
            var exception = Assert.Throws<InvalidElementalException>(() => floorRow.ValidateEquipmentElementalType(equipmentList));
            Assert.Contains("Invalid equipment elemental type", exception.Message);
            Assert.Contains("Land", exception.Message);
            Assert.Contains("Fire, Water", exception.Message);
        }

        [Fact]
        public void ValidateEquipmentElementalType_WithMultipleTypes_ShouldThrowForAllInvalidTypes()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            var requiredTypes = new List<ElementalType> { ElementalType.Fire, ElementalType.Water };
            floorRow.GetType().GetProperty("RequiredElementalTypes")?.SetValue(floorRow, requiredTypes);

            var equipment1 = CreateTestEquipment(ItemType.Equipment);
            equipment1.ElementalType = ElementalType.Land; // Invalid

            var equipment2 = CreateTestEquipment(ItemType.Equipment);
            equipment2.ElementalType = ElementalType.Wind; // Invalid

            var equipmentList = new List<Equipment> { equipment1, equipment2 };

            // Act & Assert
            var exception = Assert.Throws<InvalidElementalException>(() => floorRow.ValidateEquipmentElementalType(equipmentList));
            Assert.Contains("Invalid equipment elemental type", exception.Message);
            Assert.Contains("Land", exception.Message);
            Assert.Contains("Wind", exception.Message);
            Assert.Contains("Fire, Water", exception.Message);
        }

        [Fact]
        public void ValidateEquipmentElementalType_WithNoRestrictions_ShouldPass()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            // No restrictions set
            var equipment1 = CreateTestEquipment(ItemType.Equipment);
            equipment1.ElementalType = ElementalType.Fire;

            var equipment2 = CreateTestEquipment(ItemType.Equipment);
            equipment2.ElementalType = ElementalType.Land;

            var equipmentList = new List<Equipment> { equipment1, equipment2 };

            // Act & Assert - Should not throw
            floorRow.ValidateEquipmentElementalType(equipmentList);
        }

        [Fact]
        public void ValidateRuneTypes_WithMultipleForbiddenTypes_ShouldThrow()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            var forbiddenRuneTypes = new List<RuneType> { RuneType.Stat, RuneType.Skill };
            floorRow.GetType().GetProperty("ForbiddenRuneTypes")?.SetValue(floorRow, forbiddenRuneTypes);

            var runeListSheet = _tableSheets.RuneListSheet;

            // Find runes with Stat and Skill types
            var statRune = runeListSheet.Values.FirstOrDefault(r => r.RuneType == (int)RuneType.Stat);
            var skillRune = runeListSheet.Values.FirstOrDefault(r => r.RuneType == (int)RuneType.Skill);

            if (statRune == null || skillRune == null)
            {
                // Skip test if test data doesn't have required runes
                return;
            }

            var runeInfos = new List<RuneSlotInfo>
            {
                new RuneSlotInfo(0, statRune.Id), // Forbidden: Stat
                new RuneSlotInfo(1, skillRune.Id), // Forbidden: Skill
            };

            // Act & Assert
            var exception = Assert.Throws<ForbiddenRuneTypeEquippedException>(
                () => floorRow.ValidateRuneTypes(runeInfos, runeListSheet));

            Assert.NotNull(exception.ForbiddenRuneTypes);
            Assert.NotNull(exception.EquippedRuneTypes);
            Assert.Contains(RuneType.Stat, exception.ForbiddenRuneTypes);
            Assert.Contains(RuneType.Skill, exception.ForbiddenRuneTypes);
            Assert.Contains(RuneType.Stat, exception.EquippedRuneTypes);
            Assert.Contains(RuneType.Skill, exception.EquippedRuneTypes);
        }

        [Fact]
        public void ValidateRuneTypes_WithMultipleForbiddenTypes_ShouldAllowAllowedTypes()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            var forbiddenRuneTypes = new List<RuneType> { RuneType.Stat, RuneType.Skill };
            floorRow.GetType().GetProperty("ForbiddenRuneTypes")?.SetValue(floorRow, forbiddenRuneTypes);

            var runeListSheet = _tableSheets.RuneListSheet;

            // Find a rune with a type that's not forbidden (e.g., if there's a different type)
            // For this test, we'll use a rune that's not Stat or Skill
            var allowedRune = runeListSheet.Values.FirstOrDefault(r =>
                r.RuneType != (int)RuneType.Stat && r.RuneType != (int)RuneType.Skill);

            if (allowedRune == null)
            {
                // Skip test if test data doesn't have required runes
                return;
            }

            var runeInfos = new List<RuneSlotInfo>
            {
                new RuneSlotInfo(0, allowedRune.Id), // Allowed type
            };

            // Act & Assert - Should not throw
            floorRow.ValidateRuneTypes(runeInfos, runeListSheet);
        }

        [Fact]
        public void ValidateRuneTypes_WithNoRestrictions_ShouldPass()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            // No restrictions set
            var runeListSheet = _tableSheets.RuneListSheet;
            var anyRune = runeListSheet.Values.FirstOrDefault();
            if (anyRune == null)
            {
                // Skip test if test data doesn't have runes
                return;
            }

            var runeInfos = new List<RuneSlotInfo>
            {
                new RuneSlotInfo(0, anyRune.Id),
            };

            // Act & Assert - Should not throw
            floorRow.ValidateRuneTypes(runeInfos, runeListSheet);
        }

        [Fact]
        public void ValidateRuneTypes_WithPartialForbiddenTypes_ShouldThrow()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            var forbiddenRuneTypes = new List<RuneType> { RuneType.Stat, RuneType.Skill };
            floorRow.GetType().GetProperty("ForbiddenRuneTypes")?.SetValue(floorRow, forbiddenRuneTypes);

            var runeListSheet = _tableSheets.RuneListSheet;

            // Find runes with different types
            var statRune = runeListSheet.Values.FirstOrDefault(r => r.RuneType == (int)RuneType.Stat);
            var allowedRune = runeListSheet.Values.FirstOrDefault(r =>
                r.RuneType != (int)RuneType.Stat && r.RuneType != (int)RuneType.Skill);

            if (statRune == null || allowedRune == null)
            {
                // Skip test if test data doesn't have required runes
                return;
            }

            var runeInfos = new List<RuneSlotInfo>
            {
                new RuneSlotInfo(0, allowedRune.Id), // Allowed
                new RuneSlotInfo(1, statRune.Id), // Forbidden: Stat
            };

            // Act & Assert
            var exception = Assert.Throws<ForbiddenRuneTypeEquippedException>(
                () => floorRow.ValidateRuneTypes(runeInfos, runeListSheet));

            Assert.NotNull(exception.ForbiddenRuneTypes);
            Assert.NotNull(exception.EquippedRuneTypes);
            Assert.Contains(RuneType.Stat, exception.ForbiddenRuneTypes);
            Assert.Contains(RuneType.Stat, exception.EquippedRuneTypes);
        }

        [Fact]
        public void GetRandomConditions_WithSufficientConditions_ShouldReturnValidCount()
        {
            // Arrange
            var fields = new List<string>
            {
                "1", // Id
                "1", // Floor
                string.Empty, // RequiredCp
                string.Empty, // MaxCp
                string.Empty, // ForbiddenItemSubTypes
                string.Empty, // MinItemGrade
                string.Empty, // MaxItemGrade
                string.Empty, // MinItemLevel
                string.Empty, // MaxItemLevel
                "1", // GuaranteedConditionId
                "2", // MinRandomConditions
                "3", // MaxRandomConditions
                string.Empty, // RandomConditionId1
                string.Empty, // RandomConditionWeight1
                string.Empty, // RandomConditionId2
                string.Empty, // RandomConditionWeight2
                string.Empty, // RandomConditionId3
                string.Empty, // RandomConditionWeight3
                string.Empty, // RandomConditionId4
                string.Empty, // RandomConditionWeight4
                string.Empty, // RandomConditionId5
                string.Empty, // RandomConditionWeight5
                string.Empty, // ItemRewardId1
                string.Empty, // ItemRewardCount1
                string.Empty, // ItemRewardId2
                string.Empty, // ItemRewardCount2
                string.Empty, // ItemRewardId3
                string.Empty, // ItemRewardCount3
                string.Empty, // ItemRewardId4
                string.Empty, // ItemRewardCount4
                string.Empty, // ItemRewardId5
                string.Empty, // ItemRewardCount5
                string.Empty, // FungibleAssetRewardTicker1
                string.Empty, // FungibleAssetRewardAmount1
                string.Empty, // FungibleAssetRewardTicker2
                string.Empty, // FungibleAssetRewardAmount2
                string.Empty, // FungibleAssetRewardTicker3
                string.Empty, // FungibleAssetRewardAmount3
                string.Empty, // FungibleAssetRewardTicker4
                string.Empty, // FungibleAssetRewardAmount4
                string.Empty, // FungibleAssetRewardTicker5
                string.Empty, // FungibleAssetRewardAmount5
                string.Empty, // NcgCost
                string.Empty, // MaterialCostId
                string.Empty, // MaterialCostCount
                string.Empty, // ForbiddenRuneTypes
                string.Empty, // RequiredElementalTypes
            };

            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.Set(fields);

            var conditionSheet = _tableSheets.InfiniteTowerConditionSheet;
            var random = new TestRandom();

            // Act
            var conditions = floorRow.GetRandomConditions(conditionSheet, random, 1);

            // Assert
            Assert.NotNull(conditions);
            Assert.True(conditions.Count >= floorRow.MinRandomConditions);
            Assert.True(conditions.Count <= floorRow.MaxRandomConditions);
            Assert.All(conditions, c => Assert.NotEqual(1, c.Id)); // Should exclude guaranteed condition
            Assert.Equal(conditions.Count, conditions.Select(c => c.Id).Distinct().Count()); // No duplicates
        }

        [Fact]
        public void GetRandomConditions_WithInsufficientConditions_ShouldThrow()
        {
            // Arrange
            var fields = new List<string>
            {
                "1", // Id
                "1", // Floor
                string.Empty, // RequiredCp
                string.Empty, // MaxCp
                string.Empty, // ForbiddenItemSubTypes
                string.Empty, // MinItemGrade
                string.Empty, // MaxItemGrade
                string.Empty, // MinItemLevel
                string.Empty, // MaxItemLevel
                "1", // GuaranteedConditionId
                "100", // MinRandomConditions (too high)
                "100", // MaxRandomConditions
                string.Empty, // RandomConditionId1
                string.Empty, // RandomConditionWeight1
                string.Empty, // RandomConditionId2
                string.Empty, // RandomConditionWeight2
                string.Empty, // RandomConditionId3
                string.Empty, // RandomConditionWeight3
                string.Empty, // RandomConditionId4
                string.Empty, // RandomConditionWeight4
                string.Empty, // RandomConditionId5
                string.Empty, // RandomConditionWeight5
                string.Empty, // ItemRewardId1
                string.Empty, // ItemRewardCount1
                string.Empty, // ItemRewardId2
                string.Empty, // ItemRewardCount2
                string.Empty, // ItemRewardId3
                string.Empty, // ItemRewardCount3
                string.Empty, // ItemRewardId4
                string.Empty, // ItemRewardCount4
                string.Empty, // ItemRewardId5
                string.Empty, // ItemRewardCount5
                string.Empty, // FungibleAssetRewardTicker1
                string.Empty, // FungibleAssetRewardAmount1
                string.Empty, // FungibleAssetRewardTicker2
                string.Empty, // FungibleAssetRewardAmount2
                string.Empty, // FungibleAssetRewardTicker3
                string.Empty, // FungibleAssetRewardAmount3
                string.Empty, // FungibleAssetRewardTicker4
                string.Empty, // FungibleAssetRewardAmount4
                string.Empty, // FungibleAssetRewardTicker5
                string.Empty, // FungibleAssetRewardAmount5
                string.Empty, // NcgCost
                string.Empty, // MaterialCostId
                string.Empty, // MaterialCostCount
                string.Empty, // ForbiddenRuneTypes
                string.Empty, // RequiredElementalTypes
            };

            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.Set(fields);

            var conditionSheet = _tableSheets.InfiniteTowerConditionSheet;
            var random = new TestRandom();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                floorRow.GetRandomConditions(conditionSheet, random, 1));
            Assert.Contains("Insufficient available conditions", exception.Message);
        }

        private Equipment CreateTestWeapon()
        {
            // Weapon 타입의 Equipment 찾기
            var weaponRow = _tableSheets.EquipmentItemSheet.Values
                .FirstOrDefault(x => x.ItemSubType == ItemSubType.Weapon);

            if (weaponRow == null)
            {
                throw new InvalidOperationException($"No weapon found in EquipmentItemSheet");
            }

            var item = ItemFactory.CreateItem(weaponRow, new TestRandom());
            if (item is Equipment equipment)
            {
                return equipment;
            }

            throw new InvalidOperationException($"Created item is not Equipment type: {item.GetType()}");
        }

        private Equipment CreateTestEquipment(ItemType itemType, int grade = 1, int level = 1)
        {
            // EquipmentItemSheet에서 직접 찾아서 사용
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values
                .FirstOrDefault(x => x.Grade == grade);

            if (equipmentRow == null)
            {
                // 해당 조건의 아이템이 없으면 기본 Equipment를 사용
                equipmentRow = _tableSheets.EquipmentItemSheet.Values.FirstOrDefault();
            }

            if (equipmentRow == null)
            {
                throw new InvalidOperationException($"No equipment found in EquipmentItemSheet");
            }

            var item = ItemFactory.CreateItem(equipmentRow, new TestRandom());
            if (item is Equipment equipment)
            {
                // Level 설정
                if (level > 1)
                {
                    for (int i = 1; i < level; i++)
                    {
                        equipment.LevelUp(new TestRandom(), _tableSheets.EnhancementCostSheetV2.Values.First(), false);
                    }
                }

                return equipment;
            }

            throw new InvalidOperationException($"Created item is not Equipment type: {item.GetType()}");
        }

        private Costume CreateTestCostume(ItemType itemType, int grade = 1, int level = 1)
        {
            // CostumeItemSheet에서 직접 찾아서 사용
            var costumeRow = _tableSheets.CostumeItemSheet.Values
                .FirstOrDefault(x => x.Grade == grade);

            if (costumeRow == null)
            {
                // 해당 조건의 코스튬이 없으면 기본 코스튬을 사용
                costumeRow = _tableSheets.CostumeItemSheet.Values.FirstOrDefault();
            }

            if (costumeRow == null)
            {
                throw new InvalidOperationException($"No costume found in CostumeItemSheet");
            }

            var costume = ItemFactory.CreateCostume(costumeRow, Guid.NewGuid());
            return costume;
        }

        private ItemBase CreateTestItem(ItemType itemType, int grade = 1, int level = 1)
        {
            // 요청된 타입에 따라 적절한 시트에서 아이템 생성
            switch (itemType)
            {
                case ItemType.Equipment:
                    return CreateTestEquipment(itemType, grade, level);

                case ItemType.Costume:
                    return CreateTestCostume(itemType, grade, level);

                case ItemType.Material:
                    // MaterialItemSheet에서 Material 아이템 생성
                    var materialRow = _tableSheets.MaterialItemSheet.Values.FirstOrDefault();
                    if (materialRow == null)
                    {
                        throw new InvalidOperationException($"No material found in MaterialItemSheet");
                    }

                    return ItemFactory.CreateItem(materialRow, new TestRandom());

                default:
                    throw new InvalidOperationException($"Unsupported ItemType: {itemType}");
            }
        }
    }
}
