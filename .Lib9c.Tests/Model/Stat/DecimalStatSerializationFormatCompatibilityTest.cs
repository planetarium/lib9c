namespace Lib9c.Tests.Model.Stat
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;

    /// <summary>
    /// Tests for DecimalStat serialization format compatibility.
    /// Ensures backward compatibility between Dictionary and List formats.
    /// </summary>
    public class DecimalStatSerializationFormatCompatibilityTest
    {
        [Fact]
        public void Serialize_ReturnsListFormat()
        {
            // Arrange
            var stat = new DecimalStat(StatType.HP, 100, 50);

            // Act
            var serialized = stat.Serialize();

            // Assert
            Assert.IsType<List>(serialized);
            var list = (List)serialized;
            Assert.Equal(4, list.Count); // version + statType + baseValue + additionalValue
            Assert.Equal(1, ((Integer)list[0]).Value); // version
            Assert.IsType<Binary>(list[1]); // statType
            Assert.IsType<Text>(list[2]); // baseValue (decimal is serialized as Text)
            Assert.IsType<Text>(list[3]); // additionalValue (decimal is serialized as Text)
        }

        [Fact]
        public void Deserialize_SupportsBothFormats()
        {
            // Arrange
            var originalStat = new DecimalStat(StatType.ATK, 200, 75);

            // Act - Test List format
            var listSerialized = originalStat.Serialize();
            var deserializedFromList = new DecimalStat((IValue)listSerialized);

            // Act - Test Dictionary format (legacy)
            var dictSerialized = Dictionary.Empty
                .Add("statType", StatType.ATK.Serialize())
                .Add("value", 200m.Serialize())
                .Add("additionalValue", 75m.Serialize());
            var deserializedFromDict = new DecimalStat((IValue)dictSerialized);

            // Assert
            Assert.Equal(originalStat.StatType, deserializedFromList.StatType);
            Assert.Equal(originalStat.BaseValue, deserializedFromList.BaseValue);
            Assert.Equal(originalStat.AdditionalValue, deserializedFromList.AdditionalValue);
            Assert.Equal(originalStat.TotalValue, deserializedFromList.TotalValue);

            Assert.Equal(originalStat.StatType, deserializedFromDict.StatType);
            Assert.Equal(originalStat.BaseValue, deserializedFromDict.BaseValue);
            Assert.Equal(originalStat.AdditionalValue, deserializedFromDict.AdditionalValue);
            Assert.Equal(originalStat.TotalValue, deserializedFromDict.TotalValue);
        }

        [Fact]
        public void Deserialize_EmptyStat_WorksCorrectly()
        {
            // Arrange
            var stat = new DecimalStat(StatType.DEF);

            // Act - Test List format
            var listSerialized = stat.Serialize();
            var deserializedFromList = new DecimalStat((IValue)listSerialized);

            // Act - Test Dictionary format (legacy)
            var dictSerialized = Dictionary.Empty
                .Add("statType", StatType.DEF.Serialize())
                .Add("value", 0m.Serialize())
                .Add("additionalValue", 0m.Serialize());
            var deserializedFromDict = new DecimalStat((IValue)dictSerialized);

            // Assert
            Assert.Equal(stat.StatType, deserializedFromList.StatType);
            Assert.Equal(stat.BaseValue, deserializedFromList.BaseValue);
            Assert.Equal(stat.AdditionalValue, deserializedFromList.AdditionalValue);

            Assert.Equal(stat.StatType, deserializedFromDict.StatType);
            Assert.Equal(stat.BaseValue, deserializedFromDict.BaseValue);
            Assert.Equal(stat.AdditionalValue, deserializedFromDict.AdditionalValue);
        }

        [Fact]
        public void Deserialize_AllStatTypes_WorksCorrectly()
        {
            // Arrange
            var statTypes = new[]
            {
                StatType.HP, StatType.ATK, StatType.DEF, StatType.CRI, StatType.HIT,
                StatType.SPD, StatType.DRV, StatType.DRR, StatType.CDMG,
                StatType.ArmorPenetration, StatType.Thorn,
            };

            foreach (var statType in statTypes)
            {
                // Arrange
                var stat = new DecimalStat(statType, 100, 50);

                // Act
                var serialized = stat.Serialize();
                var deserialized = new DecimalStat((IValue)serialized);

                // Assert
                Assert.Equal(stat.StatType, deserialized.StatType);
                Assert.Equal(stat.BaseValue, deserialized.BaseValue);
                Assert.Equal(stat.AdditionalValue, deserialized.AdditionalValue);
                Assert.Equal(stat.TotalValue, deserialized.TotalValue);
            }
        }

        [Fact]
        public void Deserialize_WithOnlyBaseValue_WorksCorrectly()
        {
            // Arrange
            var stat = new DecimalStat(StatType.CRI, 25);

            // Act
            var serialized = stat.Serialize();
            var deserialized = new DecimalStat((IValue)serialized);

            // Assert
            Assert.Equal(stat.StatType, deserialized.StatType);
            Assert.Equal(stat.BaseValue, deserialized.BaseValue);
            Assert.Equal(stat.AdditionalValue, deserialized.AdditionalValue);
            Assert.Equal(stat.TotalValue, deserialized.TotalValue);
        }

        [Fact]
        public void Deserialize_WithOnlyAdditionalValue_WorksCorrectly()
        {
            // Arrange
            var stat = new DecimalStat(StatType.HIT);
            stat.SetAdditionalValue(30);

            // Act
            var serialized = stat.Serialize();
            var deserialized = new DecimalStat((IValue)serialized);

            // Assert
            Assert.Equal(stat.StatType, deserialized.StatType);
            Assert.Equal(stat.BaseValue, deserialized.BaseValue);
            Assert.Equal(stat.AdditionalValue, deserialized.AdditionalValue);
            Assert.Equal(stat.TotalValue, deserialized.TotalValue);
        }

        [Fact]
        public void Deserialize_InvalidFormat_ThrowsArgumentException()
        {
            // Arrange
            var invalidSerialized = (IValue)new Text("invalid");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new DecimalStat(invalidSerialized));
            Assert.Contains("Unsupported serialization format", exception.Message);
        }

        [Fact]
        public void Deserialize_NullValue_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new DecimalStat((IValue)null));
            Assert.Equal("serialized", exception.ParamName);
        }

        [Fact]
        public void Deserialize_InvalidListLength_ThrowsArgumentException()
        {
            // Arrange
            var invalidList = List.Empty.Add(1).Add(StatType.HP.Serialize()); // Only version and statType

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new DecimalStat((IValue)invalidList));
            Assert.Contains("Invalid list length", exception.Message);
        }

        [Fact]
        public void Deserialize_InvalidVersion_ThrowsArgumentException()
        {
            // Arrange
            var invalidVersionList = List.Empty
                .Add(999) // Invalid version
                .Add(StatType.HP.Serialize())
                .Add(100.Serialize())
                .Add(50.Serialize());

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new DecimalStat((IValue)invalidVersionList));
            Assert.Contains("Unsupported serialization version", exception.Message);
        }

        [Fact]
        public void Serialize_FieldOrder_IsCorrect()
        {
            // Arrange
            var stat = new DecimalStat(StatType.SPD, 15, 5);

            // Act
            var serialized = stat.Serialize();
            var list = (List)serialized;

            // Assert
            Assert.Equal(4, list.Count);

            // Check field order: [version, statType, baseValue, additionalValue]
            var version = ((Integer)list[0]).Value;
            var statType = StatTypeExtension.Deserialize((Binary)list[1]);
            var baseValue = list[2].ToDecimal();
            var additionalValue = list[3].ToDecimal();

            Assert.Equal(1, version);
            Assert.Equal(StatType.SPD, statType);
            Assert.Equal(15, baseValue);
            Assert.Equal(5, additionalValue);
        }

        [Fact]
        public void RoundTrip_ComplexStat_WorksCorrectly()
        {
            // Arrange
            var originalStat = new DecimalStat(StatType.CDMG, 50.5m, 25.75m);

            // Act - Round trip through List format
            var serialized = originalStat.Serialize();
            var deserialized = new DecimalStat((IValue)serialized);

            // Assert
            Assert.Equal(originalStat.StatType, deserialized.StatType);
            Assert.Equal(originalStat.BaseValue, deserialized.BaseValue);
            Assert.Equal(originalStat.AdditionalValue, deserialized.AdditionalValue);
            Assert.Equal(originalStat.TotalValue, deserialized.TotalValue);
            Assert.Equal(originalStat.BaseValueAsLong, deserialized.BaseValueAsLong);
            Assert.Equal(originalStat.AdditionalValueAsLong, deserialized.AdditionalValueAsLong);
            Assert.Equal(originalStat.TotalValueAsLong, deserialized.TotalValueAsLong);
        }

        [Fact]
        public void Deserialize_LegacyDictionaryWithoutAdditionalValue_WorksCorrectly()
        {
            // Arrange - Legacy format without additionalValue field
            var dictSerialized = Dictionary.Empty
                .Add("statType", StatType.DEF.Serialize())
                .Add("value", 30m.Serialize());

            // Act
            var deserialized = new DecimalStat((IValue)dictSerialized);

            // Assert
            Assert.Equal(StatType.DEF, deserialized.StatType);
            Assert.Equal(30, deserialized.BaseValue);
            Assert.Equal(0, deserialized.AdditionalValue); // Should default to 0
        }

        [Fact]
        public void SerializeWithoutAdditional_ReturnsDictionaryFormat()
        {
            // Arrange
            var stat = new DecimalStat(StatType.HP, 100, 50);

            // Act
            var serialized = stat.SerializeWithoutAdditional();

            // Assert
            Assert.IsType<Dictionary>(serialized);
            var dict = (Dictionary)serialized;
            Assert.Equal(2, dict.Count); // statType + value (total value)

            var statType = StatTypeExtension.Deserialize((Binary)dict["statType"]);
            var totalValue = dict["value"].ToDecimal();

            Assert.Equal(StatType.HP, statType);
            Assert.Equal(150, totalValue); // 100 + 50
        }

        [Fact]
        public void Clone_WorksCorrectly()
        {
            // Arrange
            var originalStat = new DecimalStat(StatType.ATK, 200, 75);

            // Act
            var clonedStat = (DecimalStat)originalStat.Clone();

            // Assert
            Assert.Equal(originalStat.StatType, clonedStat.StatType);
            Assert.Equal(originalStat.BaseValue, clonedStat.BaseValue);
            Assert.Equal(originalStat.AdditionalValue, clonedStat.AdditionalValue);
            Assert.Equal(originalStat.TotalValue, clonedStat.TotalValue);

            // Verify it's a deep copy
            Assert.NotSame(originalStat, clonedStat);
        }

        [Fact]
        public void Equals_WorksCorrectly()
        {
            // Arrange
            var stat1 = new DecimalStat(StatType.HP, 100, 50);
            var stat2 = new DecimalStat(StatType.HP, 100, 50);
            var stat3 = new DecimalStat(StatType.HP, 100, 25);
            var stat4 = new DecimalStat(StatType.ATK, 100, 50);

            // Act & Assert
            Assert.Equal(stat1, stat2);
            Assert.NotEqual(stat1, stat3);
            Assert.NotEqual(stat1, stat4);
            Assert.NotNull(stat1);
            Assert.Equal(stat1, stat1); // Same instance
        }

        [Fact]
        public void GetHashCode_WorksCorrectly()
        {
            // Arrange
            var stat1 = new DecimalStat(StatType.HP, 100, 50);
            var stat2 = new DecimalStat(StatType.HP, 100, 50);
            var stat3 = new DecimalStat(StatType.HP, 100, 25);

            // Act & Assert
            Assert.Equal(stat1.GetHashCode(), stat2.GetHashCode());
            Assert.NotEqual(stat1.GetHashCode(), stat3.GetHashCode());
        }
    }
}
