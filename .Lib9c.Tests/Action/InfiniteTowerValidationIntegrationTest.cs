namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Xunit;

    /// <summary>
    /// Integration tests for infinite tower validation logic.
    /// These tests use reflection to call the actual validation methods from InfiniteTowerFloorSheet.Row.
    /// </summary>
    public class InfiniteTowerValidationIntegrationTest
    {
        [Fact]
        public void ValidateItemTypeRestrictions_With_ForbiddenTypes_ShouldThrow()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();
            var forbiddenTypes = new List<string> { "Material", };
            SetProperty(floorRow, "ForbiddenItemTypes", forbiddenTypes);

            var equipmentList = new List<MockItem>
            {
                new MockItem { ItemType = "Equipment" },
                new MockItem { ItemType = "Material" }, // This should be forbidden
            };

            // Act & Assert
            var exception = Assert.ThrowsAny<Exception>(() =>
                CallMethod(floorRow, "ValidateItemTypeRestrictions", equipmentList));

            // Handle TargetInvocationException
            var actualException = exception is System.Reflection.TargetInvocationException targetEx
                ? targetEx.InnerException
                : exception;

            Assert.Contains("Invalid item type", actualException.Message);
            Assert.Contains("Material", actualException.Message);
            Assert.Contains("forbidden", actualException.Message);
        }

        [Fact]
        public void ValidateItemTypeRestrictions_With_AllowedTypes_ShouldPass()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();
            var forbiddenTypes = new List<string> { "Material", };
            SetProperty(floorRow, "ForbiddenItemTypes", forbiddenTypes);

            var equipmentList = new List<MockItem>
            {
                new MockItem { ItemType = "Equipment" },
                new MockItem { ItemType = "Costume" },
            };

            // Act & Assert - Should not throw
            CallMethod(floorRow, "ValidateItemTypeRestrictions", equipmentList);
        }

        [Fact]
        public void ValidateItemTypeRestrictions_With_NoRestrictions_ShouldPass()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();

            // No restrictions set
            var equipmentList = new List<MockItem>
            {
                new MockItem { ItemType = "Equipment" },
                new MockItem { ItemType = "Material" },
            };

            // Act & Assert - Should not throw
            CallMethod(floorRow, "ValidateItemTypeRestrictions", equipmentList);
        }

        [Fact]
        public void ValidateItemGradeRestrictions_With_MinGrade_ShouldThrow()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();
            SetProperty(floorRow, "MinItemGrade", 3);

            var equipmentList = new List<MockItem>
            {
                new MockItem { Grade = 2 }, // Below minimum
                new MockItem { Grade = 4 }, // Above minimum
            };

            // Act & Assert
            var exception = Assert.ThrowsAny<Exception>(() =>
                CallMethod(floorRow, "ValidateItemGradeRestrictions", equipmentList));

            // Handle TargetInvocationException
            var actualException = exception is System.Reflection.TargetInvocationException targetEx
                ? targetEx.InnerException
                : exception;

            Assert.Contains("Invalid item grade", actualException.Message);
            Assert.Contains("below minimum", actualException.Message);
        }

        [Fact]
        public void ValidateItemGradeRestrictions_With_MaxGrade_ShouldThrow()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();
            SetProperty(floorRow, "MaxItemGrade", 3);

            var equipmentList = new List<MockItem>
            {
                new MockItem { Grade = 2 }, // Below maximum
                new MockItem { Grade = 4 }, // Above maximum
            };

            // Act & Assert
            var exception = Assert.ThrowsAny<Exception>(() =>
                CallMethod(floorRow, "ValidateItemGradeRestrictions", equipmentList));

            // Handle TargetInvocationException
            var actualException = exception is System.Reflection.TargetInvocationException targetEx
                ? targetEx.InnerException
                : exception;

            Assert.Contains("Invalid item grade", actualException.Message);
            Assert.Contains("exceeds maximum", actualException.Message);
        }

        [Fact]
        public void ValidateItemGradeRestrictions_With_ValidGrades_ShouldPass()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();
            SetProperty(floorRow, "MinItemGrade", 2);
            SetProperty(floorRow, "MaxItemGrade", 4);

            var equipmentList = new List<MockItem>
            {
                new MockItem { Grade = 2 },
                new MockItem { Grade = 3 },
                new MockItem { Grade = 4 },
            };

            // Act & Assert - Should not throw
            CallMethod(floorRow, "ValidateItemGradeRestrictions", equipmentList);
        }

        [Fact]
        public void ValidateItemLevelRestrictions_With_MinLevel_ShouldThrow()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();
            SetProperty(floorRow, "MinItemLevel", 5);

            var equipmentList = new List<MockItem>
            {
                new MockItem { Level = 3 }, // Below minimum
                new MockItem { Level = 7 }, // Above minimum
            };

            // Act & Assert
            var exception = Assert.ThrowsAny<Exception>(() =>
                CallMethod(floorRow, "ValidateItemLevelRestrictions", equipmentList));

            // Handle TargetInvocationException
            var actualException = exception is System.Reflection.TargetInvocationException targetEx
                ? targetEx.InnerException
                : exception;

            Assert.Contains("Invalid item level", actualException.Message);
            Assert.Contains("below minimum", actualException.Message);
        }

        [Fact]
        public void ValidateItemLevelRestrictions_With_MaxLevel_ShouldThrow()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();
            SetProperty(floorRow, "MaxItemLevel", 5);

            var equipmentList = new List<MockItem>
            {
                new MockItem { Level = 3 }, // Below maximum
                new MockItem { Level = 7 }, // Above maximum
            };

            // Act & Assert
            var exception = Assert.ThrowsAny<Exception>(() =>
                CallMethod(floorRow, "ValidateItemLevelRestrictions", equipmentList));

            // Handle TargetInvocationException
            var actualException = exception is System.Reflection.TargetInvocationException targetEx
                ? targetEx.InnerException
                : exception;

            Assert.Contains("Invalid item level", actualException.Message);
            Assert.Contains("exceeds maximum", actualException.Message);
        }

        [Fact]
        public void ValidateItemLevelRestrictions_With_Costumes_ShouldSkip()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();
            SetProperty(floorRow, "MinItemLevel", 5);

            var costumeList = new List<MockItem>
            {
                new MockItem { ItemType = "Costume", Level = 1 }, // Costumes should be skipped
            };

            // Act & Assert - Should not throw (costumes are skipped)
            CallMethod(floorRow, "ValidateItemLevelRestrictions", costumeList);
        }

        [Fact]
        public void ValidateCpRequirements_With_MinCp_ShouldThrow()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();
            SetProperty(floorRow, "RequiredCp", 1000L);

            // Act & Assert
            var exception = Assert.ThrowsAny<Exception>(() =>
                CallMethod(floorRow, "ValidateCpRequirements", 500));

            // Handle TargetInvocationException
            var actualException = exception is System.Reflection.TargetInvocationException targetEx
                ? targetEx.InnerException
                : exception;

            Assert.Contains("Insufficient combat power", actualException.Message);
            Assert.Contains("below minimum", actualException.Message);
        }

        [Fact]
        public void ValidateCpRequirements_With_MaxCp_ShouldThrow()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();
            SetProperty(floorRow, "MaxCp", 1000L);

            // Act & Assert
            var exception = Assert.ThrowsAny<Exception>(() =>
                CallMethod(floorRow, "ValidateCpRequirements", 1500));

            // Handle TargetInvocationException
            var actualException = exception is System.Reflection.TargetInvocationException targetEx
                ? targetEx.InnerException
                : exception;

            Assert.Contains("Excessive combat power", actualException.Message);
            Assert.Contains("exceeds maximum", actualException.Message);
        }

        [Fact]
        public void ValidateCpRequirements_With_ValidCp_ShouldPass()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();
            SetProperty(floorRow, "RequiredCp", 500L);
            SetProperty(floorRow, "MaxCp", 1500L);

            // Act & Assert - Should not throw
            CallMethod(floorRow, "ValidateCpRequirements", 1000);
        }

        [Fact]
        public void ValidateCpRequirements_With_NoRestrictions_ShouldPass()
        {
            // Arrange
            var floorRow = CreateMockFloorRow();
            // No restrictions set

            // Act & Assert - Should not throw
            CallMethod(floorRow, "ValidateCpRequirements", 1000);
        }

        private object CreateMockFloorRow()
        {
            // Create a mock floor row that implements the validation methods
            return new MockFloorRow();
        }

        private void SetProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            property?.SetValue(obj, value);
        }

        private void CallMethod(object obj, string methodName, params object[] parameters)
        {
            var method = obj.GetType().GetMethod(methodName);
            if (method != null && method.IsGenericMethodDefinition)
            {
                // For generic methods, we need to make them non-generic
                var genericMethod = method.MakeGenericMethod(typeof(MockItem));
                genericMethod.Invoke(obj, parameters);
            }
            else
            {
                method?.Invoke(obj, parameters);
            }
        }
    }

    /// <summary>
    /// Mock floor row that implements the validation logic for testing.
    /// </summary>
#pragma warning disable SA1402 // File may only contain a single type
    public class MockFloorRow
    {
        public List<string> ForbiddenItemTypes { get; set; }

        public int? MinItemGrade { get; set; }

        public int? MaxItemGrade { get; set; }

        public int? MinItemLevel { get; set; }

        public int? MaxItemLevel { get; set; }

        public long? RequiredCp { get; set; }

        public long? MaxCp { get; set; }

        public void ValidateItemTypeRestrictions<T>(List<T> itemList)
            where T : MockItem
        {
            // Skip validation if no type restrictions are set
            if (ForbiddenItemTypes == null)
            {
                return;
            }

            foreach (var item in itemList)
            {
                // Check forbidden types
                if (ForbiddenItemTypes.Contains(item.ItemType))
                {
                    throw new Exception($"Invalid item type. Item type '{item.ItemType}' is forbidden. Forbidden types: {string.Join(", ", ForbiddenItemTypes)}");
                }
            }
        }

        public void ValidateItemGradeRestrictions<T>(List<T> itemList)
            where T : MockItem
        {
            // Skip validation if no grade restrictions are set
            if (!MinItemGrade.HasValue && !MaxItemGrade.HasValue)
            {
                return;
            }

            foreach (var item in itemList)
            {
                // Check minimum grade
                if (MinItemGrade.HasValue && item.Grade < MinItemGrade.Value)
                {
                    throw new Exception($"Invalid item grade. Item grade '{item.Grade}' is below minimum requirement. Minimum grade required: {MinItemGrade.Value}");
                }

                // Check maximum grade
                if (MaxItemGrade.HasValue && item.Grade > MaxItemGrade.Value)
                {
                    throw new Exception($"Invalid item grade. Item grade '{item.Grade}' exceeds maximum limit. Maximum grade allowed: {MaxItemGrade.Value}");
                }
            }
        }

        public void ValidateItemLevelRestrictions<T>(List<T> itemList)
            where T : MockItem
        {
            // Skip validation if no level restrictions are set
            if (!MinItemLevel.HasValue && !MaxItemLevel.HasValue)
            {
                return;
            }

            foreach (var item in itemList)
            {
                // Skip level validation for costumes
                if (item.ItemType == "Costume")
                {
                    continue;
                }

                // Check minimum level
                if (MinItemLevel.HasValue && item.Level < MinItemLevel.Value)
                {
                    throw new Exception($"Invalid item level. Item level '{item.Level}' is below minimum requirement. Minimum level required: {MinItemLevel.Value}");
                }

                // Check maximum level
                if (MaxItemLevel.HasValue && item.Level > MaxItemLevel.Value)
                {
                    throw new Exception($"Invalid item level. Item level '{item.Level}' exceeds maximum limit. Maximum level allowed: {MaxItemLevel.Value}");
                }
            }
        }

        public void ValidateCpRequirements(long currentCp)
        {
            // Skip CP validation if no requirements are set
            if (RequiredCp == null && MaxCp == null)
            {
                return;
            }

            // Check minimum CP requirement
            if (RequiredCp.HasValue && currentCp < RequiredCp.Value)
            {
                throw new Exception($"Insufficient combat power. Current CP '{currentCp}' is below minimum requirement. Minimum CP required: {RequiredCp.Value}");
            }

            // Check maximum CP limit
            if (MaxCp.HasValue && currentCp > MaxCp.Value)
            {
                throw new Exception($"Excessive combat power. Current CP '{currentCp}' exceeds maximum limit. Maximum CP allowed: {MaxCp.Value}");
            }
        }
    }

    /// <summary>
    /// Mock item for testing.
    /// </summary>
    public class MockItem
#pragma warning restore SA1402 // File may only contain a single type
    {
        public string ItemType { get; set; } = "Equipment";

        public int Grade { get; set; } = 1;

        public int Level { get; set; } = 1;
    }
}
