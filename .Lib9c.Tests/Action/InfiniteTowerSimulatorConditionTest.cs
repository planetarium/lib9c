namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using Nekoyume.Model.InfiniteTower;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.TableData;
    using Xunit;

    public class InfiniteTowerSimulatorConditionTest
    {
        [Fact]
        public void InfiniteTowerCondition_Constructor_ShouldCreateValidCondition()
        {
            // Arrange
            var conditionRow = CreateTestConditionRow();

            // Act
            var condition = new InfiniteTowerCondition(conditionRow);

            // Assert
            Assert.NotNull(condition);
            Assert.Equal(1, condition.Id);
            Assert.Equal(StatType.ATK, condition.StatType);
            Assert.Equal(50, condition.Value);
            Assert.NotNull(condition.TargetType);
            Assert.Single(condition.TargetType);
            Assert.Equal(SkillTargetType.Enemies, condition.TargetType[0]);
        }

        [Fact]
        public void InfiniteTowerCondition_GetValue_ShouldReturnCorrectValue()
        {
            // Arrange
            var conditionRow = CreateTestConditionRow();

            // Act
            var condition = new InfiniteTowerCondition(conditionRow);

            // Assert
            Assert.Equal(50, condition.GetValue());
        }

        [Fact]
        public void InfiniteTowerCondition_GetStatModifier_ShouldReturnCorrectStatModifier()
        {
            // Arrange
            var conditionRow = CreateTestConditionRow();

            // Act
            var condition = new InfiniteTowerCondition(conditionRow);
            var statModifier = condition.GetStatModifier();

            // Assert
            Assert.NotNull(statModifier);
            Assert.Equal(StatType.ATK, statModifier.StatType);
            Assert.Equal(StatModifier.OperationType.Add, statModifier.Operation);
            Assert.Equal(50, statModifier.Value);
        }

        private InfiniteTowerConditionSheet.Row CreateTestConditionRow()
        {
            var fields = new List<string>
            {
                "1", // Id
                "2", // StatType (ATK)
                "1", // TargetType (Enemies)
                "0", // OperationType (Add)
                "50", // Value
            };

            var row = new InfiniteTowerConditionSheet.Row();
            row.Set(fields);
            return row;
        }
    }
}
