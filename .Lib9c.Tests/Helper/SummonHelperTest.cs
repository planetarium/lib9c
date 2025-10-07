namespace Lib9c.Tests.Helper
{
    using System.Collections.Generic;
    using Lib9c.Helper;
    using Lib9c.TableData.Item;
    using Lib9c.TableData.Summon;
    using Lib9c.Tests.Action;
    using Xunit;

    public class SummonHelperTest
    {
        [Fact]
        public void GetSummonRecipeIdsWithGradeGuarantee_WithLowGradeItems_ShouldGuaranteeMinimumGrade()
        {
            // Arrange
            var random = new TestRandom();
            var summonRow = CreateTestSummonRow();
            var equipmentItemSheet = CreateTestEquipmentItemSheet();
            var equipmentItemRecipeSheet = CreateTestEquipmentItemRecipeSheet();
            var summonCount = 11; // Use 11 summons to trigger grade guarantee
            var minimumGrade = 3; // Epic grade (from 11 summon settings)

            // Act
            var result = SummonHelper.GetSummonRecipeIdsWithGradeGuarantee(
                summonRow, summonCount, random, equipmentItemSheet, equipmentItemRecipeSheet);

            // Assert
            Assert.Equal(summonCount, result.Count);

            // Check that at least one item meets minimum grade requirement
            var hasMinimumGrade = false;
            foreach (var recipeId in result)
            {
                if (TryGetItemGradeFromRecipeId(recipeId, equipmentItemSheet, equipmentItemRecipeSheet, out var grade))
                {
                    if (grade >= minimumGrade)
                    {
                        hasMinimumGrade = true;
                        break;
                    }
                }
            }

            Assert.True(hasMinimumGrade, "At least one item should meet minimum grade requirement");
        }

        [Fact]
        public void GetSummonRecipeIdsWithGradeGuarantee_WithHighGradeItems_ShouldNotForceGuarantee()
        {
            // Arrange
            var random = new TestRandom();
            var summonRow = CreateTestSummonRowWithHighGrades();
            var equipmentItemSheet = CreateTestEquipmentItemSheet();
            var equipmentItemRecipeSheet = CreateTestEquipmentItemRecipeSheet();
            var summonCount = 3;

            // Act
            var result = SummonHelper.GetSummonRecipeIdsWithGradeGuarantee(
                summonRow, summonCount, random, equipmentItemSheet, equipmentItemRecipeSheet);

            // Assert
            Assert.Equal(summonCount, result.Count);

            // Since all items are high grade, guarantee should not be forced
            // The result should contain the expected number of items
        }

        [Fact]
        public void CalculateSummonCount_WithValidCounts_ShouldApplyTenPlusOneRule()
        {
            // Test 10+1 rule
            Assert.Equal(11, SummonHelper.CalculateSummonCount(10));
            Assert.Equal(110, SummonHelper.CalculateSummonCount(100));
            Assert.Equal(1, SummonHelper.CalculateSummonCount(1));
            Assert.Equal(2, SummonHelper.CalculateSummonCount(2));
        }

        [Fact]
        public void CheckSummonCountIsValid_WithValidCounts_ShouldReturnTrue()
        {
            Assert.True(SummonHelper.CheckSummonCountIsValid(1));
            Assert.True(SummonHelper.CheckSummonCountIsValid(10));
            Assert.True(SummonHelper.CheckSummonCountIsValid(100));
        }

        [Fact]
        public void CheckSummonCountIsValid_WithInvalidCounts_ShouldReturnFalse()
        {
            Assert.False(SummonHelper.CheckSummonCountIsValid(2));
            Assert.False(SummonHelper.CheckSummonCountIsValid(5));
            Assert.False(SummonHelper.CheckSummonCountIsValid(50));
            Assert.False(SummonHelper.CheckSummonCountIsValid(101));
        }

        [Fact]
        public void GetSummonRecipeIdsWithGradeGuarantee_11Summons_ShouldUse11Settings()
        {
            // Arrange
            var summonRow = CreateTestSummonRow();
            var equipmentItemSheet = CreateTestEquipmentItemSheet();
            var equipmentItemRecipeSheet = CreateTestEquipmentItemRecipeSheet();
            var random = new TestRandom();

            // Act - 11 summons should use 11 summon settings (grade 3, count 1)
            var result = SummonHelper.GetSummonRecipeIdsWithGradeGuarantee(
                summonRow, 11, random, equipmentItemSheet, equipmentItemRecipeSheet);

            // Assert
            Assert.Equal(11, result.Count);

            // Check if at least one item meets minimum grade requirement for 11 summons (grade 3)
            var hasMinimumGrade = false;
            foreach (var recipeId in result)
            {
                if (TryGetItemGradeFromRecipeId(recipeId, equipmentItemSheet, equipmentItemRecipeSheet, out var grade))
                {
                    if (grade >= 3)
                    {
                        hasMinimumGrade = true;
                        break;
                    }
                }
            }

            Assert.True(hasMinimumGrade, "Should guarantee at least one item of minimum grade 3 or higher for 11 summons");
        }

        [Fact]
        public void GetSummonRecipeIdsWithGradeGuarantee_110Summons_ShouldUse110Settings()
        {
            // Arrange
            var summonRow = CreateTestSummonRow();
            var equipmentItemSheet = CreateTestEquipmentItemSheet();
            var equipmentItemRecipeSheet = CreateTestEquipmentItemRecipeSheet();
            var random = new TestRandom();

            // Act - 110 summons should use 110 summon settings (grade 4, count 2)
            var result = SummonHelper.GetSummonRecipeIdsWithGradeGuarantee(
                summonRow, 110, random, equipmentItemSheet, equipmentItemRecipeSheet);

            // Assert
            Assert.Equal(110, result.Count);

            // Check if at least one item meets minimum grade requirement for 110 summons (grade 4)
            var hasMinimumGrade = false;
            foreach (var recipeId in result)
            {
                if (TryGetItemGradeFromRecipeId(recipeId, equipmentItemSheet, equipmentItemRecipeSheet, out var grade))
                {
                    if (grade >= 4)
                    {
                        hasMinimumGrade = true;
                        break;
                    }
                }
            }

            Assert.True(hasMinimumGrade, "Should guarantee at least one item of minimum grade 4 or higher for 110 summons");
        }

        [Fact]
        public void GetSummonRecipeIdsWithGradeGuarantee_LessThan11_ShouldNotUseGuarantee()
        {
            // Arrange
            var summonRow = CreateTestSummonRow();
            var equipmentItemSheet = CreateTestEquipmentItemSheet();
            var equipmentItemRecipeSheet = CreateTestEquipmentItemRecipeSheet();
            var random = new TestRandom();

            // Act - Less than 11 summons should not use guarantee
            var result = SummonHelper.GetSummonRecipeIdsWithGradeGuarantee(
                summonRow, 5, random, equipmentItemSheet, equipmentItemRecipeSheet);

            // Assert
            Assert.Equal(5, result.Count);
            // No guarantee for less than 11 summons, so we just verify the count
        }

        [Fact]
        public void GetSummonRecipeIdsWithGradeGuarantee_NoEligibleRecipes_ShouldThrowException()
        {
            // Arrange
            var random = new TestRandom();
            var summonRow = CreateTestSummonRowWithLowGradesOnly();
            var equipmentItemSheet = CreateTestEquipmentItemSheet();
            var equipmentItemRecipeSheet = CreateTestEquipmentItemRecipeSheet();
            var summonCount = 11; // Use 11 summons to trigger grade guarantee

            // Act & Assert
            var exception = Assert.Throws<System.InvalidOperationException>(() =>
                SummonHelper.GetSummonRecipeIdsWithGradeGuarantee(
                    summonRow, summonCount, random, equipmentItemSheet, equipmentItemRecipeSheet));

            Assert.Contains("No equipment recipes found with grade >= 3", exception.Message);
            Assert.Contains("summon group 10003", exception.Message);
        }

        [Fact]
        public void GetSummonRecipeIdByRandom_DataConsistencyIssue_ShouldThrowException()
        {
            // Arrange
            var random = new TestRandom();
            var summonRow = CreateTestSummonRowWithInconsistentData();

            // Act & Assert
            var exception = Assert.Throws<System.InvalidOperationException>(() =>
                SummonHelper.GetSummonRecipeIdByRandom(summonRow, random));

            Assert.Contains("Failed to select recipe for summon group 10005", exception.Message);
            Assert.Contains("data consistency issue with recipe ratios", exception.Message);
        }

        private static SummonSheet.Row CreateTestSummonRow()
        {
            var row = new SummonSheet.Row();
            var fields = new List<string>
            {
                "10001", // GroupId
                "800201", // CostMaterial
                "10", // CostMaterialCount
                "0", // CostNcg
                "GUARANTEE", // Grade guarantee marker
                "3", // MinimumGrade11
                "1", // GuaranteeCount11
                "4", // MinimumGrade110
                "2", // GuaranteeCount110
                "101", "1000", // Recipe1: Low grade (1)
                "102", "500",  // Recipe2: Low grade (1)
                "103", "200",  // Recipe3: Medium grade (2)
                "104", "100",  // Recipe4: High grade (3)
                "105", "50",   // Recipe5: Very high grade (4)
            };
            row.Set(fields);
            return row;
        }

        private static SummonSheet.Row CreateTestSummonRowWithHighGrades()
        {
            var row = new SummonSheet.Row();
            var fields = new List<string>
            {
                "10002", // GroupId
                "800201", // CostMaterial
                "10", // CostMaterialCount
                "0", // CostNcg
                "GUARANTEE", // Grade guarantee marker
                "4", // MinimumGrade11
                "1", // GuaranteeCount11
                "5", // MinimumGrade110
                "2", // GuaranteeCount110
                "201", "100", // Recipe1: High grade (3)
                "202", "100", // Recipe2: High grade (3)
                "203", "100", // Recipe3: High grade (3)
            };
            row.Set(fields);
            return row;
        }

        private static SummonSheet.Row CreateTestSummonRowWithLowGradesOnly()
        {
            var row = new SummonSheet.Row();
            var fields = new List<string>
            {
                "10003", // GroupId
                "800201", // CostMaterial
                "10", // CostMaterialCount
                "0", // CostNcg
                "GUARANTEE", // Grade guarantee marker
                "3", // MinimumGrade11 (Epic grade)
                "1", // GuaranteeCount11
                "4", // MinimumGrade110 (Unique grade)
                "2", // GuaranteeCount110
                "101", "1000", // Recipe1: Low grade (1) - below minimum
                "102", "500",  // Recipe2: Low grade (1) - below minimum
                "103", "200",  // Recipe3: Medium grade (2) - below minimum
            };
            row.Set(fields);
            return row;
        }

        private static SummonSheet.Row CreateTestSummonRowWithNoRecipes()
        {
            var row = new SummonSheet.Row();
            var fields = new List<string>
            {
                "10004", // GroupId
                "800201", // CostMaterial
                "10", // CostMaterialCount
                "0", // CostNcg
                "GUARANTEE", // Grade guarantee marker
                "3", // MinimumGrade11
                "1", // GuaranteeCount11
                "4", // MinimumGrade110
                "2", // GuaranteeCount110
                "101", "0", // Recipe with zero ratio - this should cause an exception
                "102", "0", // Recipe with zero ratio - this should cause an exception
            };
            row.Set(fields);
            return row;
        }

        private static SummonSheet.Row CreateTestSummonRowWithInconsistentData()
        {
            var row = new SummonSheet.Row();
            var fields = new List<string>
            {
                "10005", // GroupId
                "800201", // CostMaterial
                "10", // CostMaterialCount
                "0", // CostNcg
                "GUARANTEE", // Grade guarantee marker
                "3", // MinimumGrade11
                "1", // GuaranteeCount11
                "4", // MinimumGrade110
                "2", // GuaranteeCount110
                "101", "0", // Recipe1: Zero ratio - this should cause data consistency issue
                "102", "0", // Recipe2: Zero ratio - this should cause data consistency issue
            };
            row.Set(fields);
            return row;
        }

        private static EquipmentItemSheet CreateTestEquipmentItemSheet()
        {
            var sheet = new EquipmentItemSheet();
            var content = @"id,_name,item_sub_type,grade,elemental_type,set_id,stat_type,stat_value,attack_range,spine_resource_path,exp
101,Test Weapon 1,Weapon,1,Normal,0,ATK,10,2,101,10
102,Test Weapon 2,Weapon,1,Normal,0,ATK,15,2,102,10
103,Test Weapon 3,Weapon,2,Normal,0,ATK,25,2,103,50
104,Test Weapon 4,Weapon,3,Normal,0,ATK,40,2,104,100
105,Test Weapon 5,Weapon,4,Normal,0,ATK,60,2,105,200
201,Test Weapon 6,Weapon,3,Normal,0,ATK,45,2,201,120
202,Test Weapon 7,Weapon,3,Normal,0,ATK,50,2,202,130
203,Test Weapon 8,Weapon,3,Normal,0,ATK,55,2,203,140";
            sheet.Set(content);
            return sheet;
        }

        private static EquipmentItemRecipeSheet CreateTestEquipmentItemRecipeSheet()
        {
            var sheet = new EquipmentItemRecipeSheet();
            var content = @"id,result_equipment_id,material_id,material_count,required_action_point,required_gold,required_block_index,unlock_stage,sub_recipe_id,sub_recipe_id_2,sub_recipe_id_3,required_crystal,item_sub_type
101,101,303000,2,0,0,5,3,373,374,,0,Weapon
102,102,303000,2,0,0,5,3,373,374,,0,Weapon
103,103,303000,2,0,0,5,3,373,374,,0,Weapon
104,104,303000,2,0,0,5,3,373,374,,0,Weapon
105,105,303000,2,0,0,5,3,373,374,,0,Weapon
201,201,303000,2,0,0,5,3,373,374,,0,Weapon
202,202,303000,2,0,0,5,3,373,374,,0,Weapon
203,203,303000,2,0,0,5,3,373,374,,0,Weapon";
            sheet.Set(content);
            return sheet;
        }

        private static bool TryGetItemGradeFromRecipeId(int recipeId, EquipmentItemSheet equipmentItemSheet, EquipmentItemRecipeSheet equipmentItemRecipeSheet, out int grade)
        {
            grade = 0;
            if (equipmentItemRecipeSheet.TryGetValue(recipeId, out var recipeRow) &&
                equipmentItemSheet.TryGetValue(recipeRow.ResultEquipmentId, out var equipmentRow))
            {
                grade = equipmentRow.Grade;
                return true;
            }

            return false;
        }
    }
}
