namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.InfiniteTower;
    using Nekoyume.Model.Item;
    using Xunit;

    public class InfiniteTowerBattleConditionTest
    {
        [Fact]
        public void CP_Constructor_ShouldCreateCorrectCondition()
        {
            // Arrange
            var requiredCp = 1000L;
            var maxCp = 5000L;

            // Act
            var condition = new InfiniteTowerBattleCondition(requiredCp, maxCp);

            // Assert
            Assert.Equal(BattleConditionType.CP, condition.Type);
            Assert.Equal(requiredCp, condition.RequiredCp);
            Assert.Equal(maxCp, condition.MaxCp);
            Assert.True(condition.HasRestrictions());
        }

        [Fact]
        public void CP_Constructor_WithNullValues_ShouldCreateConditionWithoutRestrictions()
        {
            // Act
            var condition = new InfiniteTowerBattleCondition((long?)null, (long?)null);

            // Assert
            Assert.Equal(BattleConditionType.CP, condition.Type);
            Assert.Null(condition.RequiredCp);
            Assert.Null(condition.MaxCp);
            Assert.False(condition.HasRestrictions());
        }

        [Fact]
        public void ItemGrade_Constructor_ShouldCreateCorrectCondition()
        {
            // Arrange
            var minGrade = 3;
            var maxGrade = 7;

            // Act
            var condition = new InfiniteTowerBattleCondition(minGrade, maxGrade, true);

            // Assert
            Assert.Equal(BattleConditionType.ItemGrade, condition.Type);
            Assert.Equal(minGrade, condition.MinItemGrade);
            Assert.Equal(maxGrade, condition.MaxItemGrade);
            Assert.True(condition.HasRestrictions());
        }

        [Fact]
        public void ItemLevel_Constructor_ShouldCreateCorrectCondition()
        {
            // Arrange
            var minLevel = 5;
            var maxLevel = 10;

            // Act
            var condition = new InfiniteTowerBattleCondition(minLevel, maxLevel);

            // Assert
            Assert.Equal(BattleConditionType.ItemLevel, condition.Type);
            Assert.Equal(minLevel, condition.MinItemLevel);
            Assert.Equal(maxLevel, condition.MaxItemLevel);
            Assert.True(condition.HasRestrictions());
        }

        [Fact]
        public void ForbiddenRuneTypes_Constructor_ShouldCreateCorrectCondition()
        {
            // Arrange
            var forbiddenTypes = new List<RuneType> { RuneType.Stat, RuneType.Skill };

            // Act
            var condition = new InfiniteTowerBattleCondition(forbiddenTypes);

            // Assert
            Assert.Equal(BattleConditionType.ForbiddenRuneTypes, condition.Type);
            Assert.Equal(forbiddenTypes, condition.ForbiddenRuneTypes);
            Assert.True(condition.HasRestrictions());
        }

        [Fact]
        public void ForbiddenRuneTypes_Constructor_WithNull_ShouldCreateEmptyList()
        {
            // Act
            var condition = new InfiniteTowerBattleCondition(null);

            // Assert
            Assert.Equal(BattleConditionType.ForbiddenRuneTypes, condition.Type);
            Assert.NotNull(condition.ForbiddenRuneTypes);
            Assert.Empty(condition.ForbiddenRuneTypes);
            Assert.False(condition.HasRestrictions());
        }

        [Fact]
        public void RequiredElementalType_Constructor_ShouldCreateCorrectCondition()
        {
            // Arrange
            var requiredType = ElementalType.Fire;

            // Act
            var condition = new InfiniteTowerBattleCondition(requiredType, true);

            // Assert
            Assert.Equal(BattleConditionType.RequiredElementalType, condition.Type);
            Assert.Equal(requiredType, condition.RequiredElementalType);
            Assert.True(condition.HasRestrictions());
        }

        [Fact]
        public void RequiredElementalType_Constructor_WithNull_ShouldCreateConditionWithoutRestrictions()
        {
            // Act
            var condition = new InfiniteTowerBattleCondition((ElementalType?)null, true);

            // Assert
            Assert.Equal(BattleConditionType.RequiredElementalType, condition.Type);
            Assert.Null(condition.RequiredElementalType);
            Assert.False(condition.HasRestrictions());
        }

        [Fact]
        public void ForbiddenItemSubTypes_Constructor_ShouldCreateCorrectCondition()
        {
            // Arrange
            var forbiddenSubTypes = new List<ItemSubType> { ItemSubType.Armor, ItemSubType.Belt };

            // Act
            var condition = new InfiniteTowerBattleCondition(forbiddenSubTypes, true);

            // Assert
            Assert.Equal(BattleConditionType.ForbiddenItemSubTypes, condition.Type);
            Assert.Equal(forbiddenSubTypes, condition.ForbiddenItemSubTypes);
            Assert.True(condition.HasRestrictions());
        }

        [Fact]
        public void ForbiddenItemSubTypes_Constructor_WithNull_ShouldCreateEmptyList()
        {
            // Act
            var condition = new InfiniteTowerBattleCondition((List<ItemSubType>)null, true);

            // Assert
            Assert.Equal(BattleConditionType.ForbiddenItemSubTypes, condition.Type);
            Assert.NotNull(condition.ForbiddenItemSubTypes);
            Assert.Empty(condition.ForbiddenItemSubTypes);
            Assert.False(condition.HasRestrictions());
        }

        [Fact]
        public void ForbiddenItemSubTypes_Constructor_WithFalseFlag_ShouldThrowException()
        {
            // Arrange
            var forbiddenSubTypes = new List<ItemSubType> { ItemSubType.Armor };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new InfiniteTowerBattleCondition(forbiddenSubTypes, false));
        }

        [Fact]
        public void RequiredElementalType_Constructor_WithFalseFlag_ShouldThrowException()
        {
            // Arrange
            ElementalType? requiredType = ElementalType.Fire;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new InfiniteTowerBattleCondition(requiredType, false));
        }

        [Fact]
        public void ItemGrade_Constructor_WithInvalidType_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new InfiniteTowerBattleCondition(1, 5, false));
        }

        [Fact]
        public void ToString_ShouldReturnCorrectDescription()
        {
            // Arrange
            var cpCondition = new InfiniteTowerBattleCondition(1000L, 5000L);
            var itemGradeCondition = new InfiniteTowerBattleCondition(3, 7, true);
            var itemLevelCondition = new InfiniteTowerBattleCondition(5, 10);
            var runeTypesCondition = new InfiniteTowerBattleCondition(new List<RuneType> { RuneType.Stat });
            var elementalCondition = new InfiniteTowerBattleCondition(ElementalType.Fire, true);
            var itemSubTypesCondition = new InfiniteTowerBattleCondition(new List<ItemSubType> { ItemSubType.Armor }, true);

            // Act & Assert
            Assert.Equal("CP: Required=1000, Max=5000", cpCondition.ToString());
            Assert.Equal("ItemGrade: Min=3, Max=7", itemGradeCondition.ToString());
            Assert.Equal("ItemLevel: Min=5, Max=10", itemLevelCondition.ToString());
            Assert.Equal("ForbiddenRuneTypes: Stat", runeTypesCondition.ToString());
            Assert.Equal("RequiredElementalType: Fire", elementalCondition.ToString());
            Assert.Equal("ForbiddenItemSubTypes: Armor", itemSubTypesCondition.ToString());
        }
    }
}
