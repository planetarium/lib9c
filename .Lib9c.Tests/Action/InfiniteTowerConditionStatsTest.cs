namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Nekoyume.Model.Stat;
    using Xunit;

    public class InfiniteTowerConditionStatsTest
    {
        [Fact]
        public void StatModifier_Constructor_ShouldCreateValidModifier()
        {
            // Arrange & Act
            var modifier = new StatModifier(StatType.HP, StatModifier.OperationType.Add, 100);

            // Assert
            Assert.Equal(StatType.HP, modifier.StatType);
            Assert.Equal(StatModifier.OperationType.Add, modifier.Operation);
            Assert.Equal(100, modifier.Value);
        }

        [Fact]
        public void StatModifier_WithPercentageOperation_ShouldCreatePercentageModifier()
        {
            // Arrange & Act
            var modifier = new StatModifier(StatType.ATK, StatModifier.OperationType.Percentage, 15);

            // Assert
            Assert.Equal(StatType.ATK, modifier.StatType);
            Assert.Equal(StatModifier.OperationType.Percentage, modifier.Operation);
            Assert.Equal(15, modifier.Value);
        }

        [Fact]
        public void StatModifier_WithZeroValue_ShouldCreateModifierWithZeroValue()
        {
            // Arrange & Act
            var modifier = new StatModifier(StatType.DEF, StatModifier.OperationType.Add, 0);

            // Assert
            Assert.Equal(StatType.DEF, modifier.StatType);
            Assert.Equal(StatModifier.OperationType.Add, modifier.Operation);
            Assert.Equal(0, modifier.Value);
        }

        [Fact]
        public void StatModifier_WithNegativeValue_ShouldCreateModifierWithNegativeValue()
        {
            // Arrange & Act
            var modifier = new StatModifier(StatType.SPD, StatModifier.OperationType.Add, -10);

            // Assert
            Assert.Equal(StatType.SPD, modifier.StatType);
            Assert.Equal(StatModifier.OperationType.Add, modifier.Operation);
            Assert.Equal(-10, modifier.Value);
        }

        [Fact]
        public void StatModifier_WithAllStatTypes_ShouldCreateModifiersForAllTypes()
        {
            // Arrange & Act
            var hpModifier = new StatModifier(StatType.HP, StatModifier.OperationType.Add, 100);
            var atkModifier = new StatModifier(StatType.ATK, StatModifier.OperationType.Add, 50);
            var defModifier = new StatModifier(StatType.DEF, StatModifier.OperationType.Add, 30);
            var spdModifier = new StatModifier(StatType.SPD, StatModifier.OperationType.Add, 20);
            var criModifier = new StatModifier(StatType.CRI, StatModifier.OperationType.Add, 5);
            var hitModifier = new StatModifier(StatType.HIT, StatModifier.OperationType.Add, 10);

            // Assert
            Assert.Equal(StatType.HP, hpModifier.StatType);
            Assert.Equal(StatType.ATK, atkModifier.StatType);
            Assert.Equal(StatType.DEF, defModifier.StatType);
            Assert.Equal(StatType.SPD, spdModifier.StatType);
            Assert.Equal(StatType.CRI, criModifier.StatType);
            Assert.Equal(StatType.HIT, hitModifier.StatType);
        }

        [Fact]
        public void StatModifier_WithAllOperationTypes_ShouldCreateModifiersForAllOperations()
        {
            // Arrange & Act
            var addModifier = new StatModifier(StatType.HP, StatModifier.OperationType.Add, 100);
            var percentageModifier = new StatModifier(StatType.ATK, StatModifier.OperationType.Percentage, 15);

            // Assert
            Assert.Equal(StatModifier.OperationType.Add, addModifier.Operation);
            Assert.Equal(StatModifier.OperationType.Percentage, percentageModifier.Operation);
        }

        [Fact]
        public void StatModifier_Equality_ShouldCompareCorrectly()
        {
            // Arrange
            var modifier1 = new StatModifier(StatType.HP, StatModifier.OperationType.Add, 100);
            var modifier2 = new StatModifier(StatType.HP, StatModifier.OperationType.Add, 100);
            var modifier3 = new StatModifier(StatType.ATK, StatModifier.OperationType.Add, 100);

            // Act & Assert
            Assert.Equal(modifier1.StatType, modifier2.StatType);
            Assert.Equal(modifier1.Operation, modifier2.Operation);
            Assert.Equal(modifier1.Value, modifier2.Value);
            Assert.NotEqual(modifier1.StatType, modifier3.StatType);
        }

        [Fact]
        public void StatModifier_ToString_ShouldReturnDescriptiveString()
        {
            // Arrange
            var modifier = new StatModifier(StatType.HP, StatModifier.OperationType.Add, 100);

            // Act
            var result = modifier.ToString();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("+100", result);
        }
    }
}
