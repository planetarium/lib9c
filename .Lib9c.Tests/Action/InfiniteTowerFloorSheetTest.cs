namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
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
                "1", // InfiniteTowerId
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
    }
}
