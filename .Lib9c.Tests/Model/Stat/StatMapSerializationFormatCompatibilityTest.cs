namespace Lib9c.Tests.Model.Stat
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Lib9c.Model.Stat;
    using Lib9c.Model.State;
    using Xunit;

    /// <summary>
    /// Tests for StatMap serialization format compatibility.
    /// Ensures backward compatibility between Dictionary and List formats.
    /// </summary>
    public class StatMapSerializationFormatCompatibilityTest
    {
        [Fact]
        public void Serialize_ReturnsListFormat()
        {
            // Arrange
            var statMap = new StatMap();
            statMap[StatType.HP].SetBaseValue(100);
            statMap[StatType.ATK].SetBaseValue(50);
            statMap[StatType.DEF].SetAdditionalValue(25);

            // Act
            var serialized = statMap.Serialize();

            // Assert
            Assert.IsType<List>(serialized);
            var list = (List)serialized;
            Assert.Equal(2, list.Count); // version + stats
            Assert.Equal(1, ((Integer)list[0]).Value); // version
            Assert.IsType<List>(list[1]); // stats
        }

        [Fact]
        public void Deserialize_SupportsBothFormats()
        {
            // Arrange
            var statMap = new StatMap();
            statMap[StatType.HP].SetBaseValue(100);
            statMap[StatType.ATK].SetBaseValue(50);
            statMap[StatType.DEF].SetAdditionalValue(25);

            // Act - Test List format
            var listSerialized = statMap.Serialize();
            var deserializedFromList = new StatMap((IValue)listSerialized);

            // Act - Test Dictionary format (legacy)
            var legacyHpStatDict = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text)"statType", StatType.HP.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"value", 100m.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"additionalValue", 0m.Serialize()),
            });
            var legacyAtkStatDict = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text)"statType", StatType.ATK.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"value", 50m.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"additionalValue", 0m.Serialize()),
            });
            var legacyDefStatDict = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text)"statType", StatType.DEF.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"value", 0m.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"additionalValue", 25m.Serialize()),
            });

            var dictSerialized = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>(StatType.HP.Serialize(), legacyHpStatDict),
                new KeyValuePair<IKey, IValue>(StatType.ATK.Serialize(), legacyAtkStatDict),
                new KeyValuePair<IKey, IValue>(StatType.DEF.Serialize(), legacyDefStatDict),
            });
            var deserializedFromDict = new StatMap((IValue)dictSerialized);

            // Assert
            Assert.Equal(statMap.HP, deserializedFromList.HP);
            Assert.Equal(statMap.ATK, deserializedFromList.ATK);
            Assert.Equal(statMap.DEF, deserializedFromList.DEF);
            Assert.Equal(statMap.BaseHP, deserializedFromList.BaseHP);
            Assert.Equal(statMap.BaseATK, deserializedFromList.BaseATK);
            Assert.Equal(statMap.AdditionalDEF, deserializedFromList.AdditionalDEF);

            Assert.Equal(statMap.HP, deserializedFromDict.HP);
            Assert.Equal(statMap.ATK, deserializedFromDict.ATK);
            Assert.Equal(statMap.DEF, deserializedFromDict.DEF);
            Assert.Equal(statMap.BaseHP, deserializedFromDict.BaseHP);
            Assert.Equal(statMap.BaseATK, deserializedFromDict.BaseATK);
            Assert.Equal(statMap.AdditionalDEF, deserializedFromDict.AdditionalDEF);
        }

        [Fact]
        public void Deserialize_EmptyStatMap_WorksCorrectly()
        {
            // Arrange
            var statMap = new StatMap();

            // Act - Test List format
            var listSerialized = statMap.Serialize();
            var deserializedFromList = new StatMap((IValue)listSerialized);

            // Act - Test Dictionary format (legacy)
            var dictSerialized = (IValue)Dictionary.Empty;
            var deserializedFromDict = new StatMap(dictSerialized);

            // Assert
            Assert.Equal(statMap.HP, deserializedFromList.HP);
            Assert.Equal(statMap.ATK, deserializedFromList.ATK);
            Assert.Equal(statMap.DEF, deserializedFromList.DEF);

            Assert.Equal(statMap.HP, deserializedFromDict.HP);
            Assert.Equal(statMap.ATK, deserializedFromDict.ATK);
            Assert.Equal(statMap.DEF, deserializedFromDict.DEF);
        }

        [Fact]
        public void Deserialize_AllStatTypes_WorksCorrectly()
        {
            // Arrange
            var statMap = new StatMap();
            statMap[StatType.HP].SetBaseValue(100);
            statMap[StatType.ATK].SetBaseValue(50);
            statMap[StatType.DEF].SetBaseValue(30);
            statMap[StatType.CRI].SetBaseValue(10);
            statMap[StatType.HIT].SetBaseValue(20);
            statMap[StatType.SPD].SetBaseValue(15);
            statMap[StatType.DRV].SetBaseValue(5);
            statMap[StatType.DRR].SetBaseValue(8);
            statMap[StatType.CDMG].SetBaseValue(25);
            statMap[StatType.ArmorPenetration].SetBaseValue(12);
            statMap[StatType.Thorn].SetBaseValue(3);

            // Act
            var serialized = statMap.Serialize();
            var deserialized = new StatMap((IValue)serialized);

            // Assert
            Assert.Equal(statMap.HP, deserialized.HP);
            Assert.Equal(statMap.ATK, deserialized.ATK);
            Assert.Equal(statMap.DEF, deserialized.DEF);
            Assert.Equal(statMap.CRI, deserialized.CRI);
            Assert.Equal(statMap.HIT, deserialized.HIT);
            Assert.Equal(statMap.SPD, deserialized.SPD);
            Assert.Equal(statMap.DRV, deserialized.DRV);
            Assert.Equal(statMap.DRR, deserialized.DRR);
            Assert.Equal(statMap.CDMG, deserialized.CDMG);
            Assert.Equal(statMap.ArmorPenetration, deserialized.ArmorPenetration);
            Assert.Equal(statMap.Thorn, deserialized.Thorn);
        }

        [Fact]
        public void Deserialize_WithAdditionalValues_WorksCorrectly()
        {
            // Arrange
            var statMap = new StatMap();
            statMap[StatType.HP].SetBaseValue(100);
            statMap[StatType.HP].SetAdditionalValue(50);
            statMap[StatType.ATK].SetAdditionalValue(25);

            // Act
            var serialized = statMap.Serialize();
            var deserialized = new StatMap((IValue)serialized);

            // Assert
            Assert.Equal(statMap.HP, deserialized.HP);
            Assert.Equal(statMap.BaseHP, deserialized.BaseHP);
            Assert.Equal(statMap.AdditionalHP, deserialized.AdditionalHP);
            Assert.Equal(statMap.ATK, deserialized.ATK);
            Assert.Equal(statMap.BaseATK, deserialized.BaseATK);
            Assert.Equal(statMap.AdditionalATK, deserialized.AdditionalATK);
        }

        [Fact]
        public void Deserialize_InvalidFormat_ThrowsArgumentException()
        {
            // Arrange
            var invalidSerialized = (IValue)new Text("invalid");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new StatMap(invalidSerialized));
            Assert.Contains("Unsupported serialization format", exception.Message);
        }

        [Fact]
        public void Deserialize_InvalidListLength_ThrowsArgumentException()
        {
            // Arrange
            var invalidList = List.Empty.Add(1); // Only version, missing stats

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new StatMap((IValue)invalidList));
            Assert.Contains("Invalid list length", exception.Message);
        }

        [Fact]
        public void Deserialize_InvalidVersion_ThrowsArgumentException()
        {
            // Arrange
            var invalidVersionList = List.Empty
                .Add(999) // Invalid version
                .Add(List.Empty); // Empty stats

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new StatMap((IValue)invalidVersionList));
            Assert.Contains("Unsupported serialization version", exception.Message);
        }

        [Fact]
        public void Serialize_OnlyNonZeroStats_AreIncluded()
        {
            // Arrange
            var statMap = new StatMap();
            statMap[StatType.HP].SetBaseValue(100);
            statMap[StatType.ATK].SetBaseValue(0); // Zero value
            statMap[StatType.DEF].SetAdditionalValue(25);

            // Act
            var serialized = statMap.Serialize();
            var deserialized = new StatMap((IValue)serialized);

            // Assert
            var list = (List)serialized;
            var statsList = (List)list[1];

            // Should only include HP and DEF (non-zero values)
            Assert.Equal(2, statsList.Count);

            // Verify the deserialized values
            Assert.Equal(100, deserialized.HP);
            Assert.Equal(0, deserialized.ATK); // Should be 0 (default)
            Assert.Equal(25, deserialized.DEF);
        }

        [Fact]
        public void Serialize_StatsAreOrderedByStatType()
        {
            // Arrange
            var statMap = new StatMap();
            statMap[StatType.Thorn].SetBaseValue(3);
            statMap[StatType.HP].SetBaseValue(100);
            statMap[StatType.ATK].SetBaseValue(50);

            // Act
            var serialized = statMap.Serialize();
            var list = (List)serialized;
            var statsList = (List)list[1];

            // Assert
            Assert.Equal(3, statsList.Count);

            // Stats should be ordered by StatType enum value
            var firstStat = new DecimalStat(statsList[0]);
            var secondStat = new DecimalStat(statsList[1]);
            var thirdStat = new DecimalStat(statsList[2]);

            // HP (0) < ATK (1) < Thorn (10)
            Assert.Equal(StatType.HP, firstStat.StatType);
            Assert.Equal(StatType.ATK, secondStat.StatType);
            Assert.Equal(StatType.Thorn, thirdStat.StatType);
        }

        [Fact]
        public void RoundTrip_ComplexStatMap_WorksCorrectly()
        {
            // Arrange
            var originalStatMap = new StatMap();
            originalStatMap[StatType.HP].SetBaseValue(1000);
            originalStatMap[StatType.HP].SetAdditionalValue(500);
            originalStatMap[StatType.ATK].SetBaseValue(200);
            originalStatMap[StatType.DEF].SetAdditionalValue(150);
            originalStatMap[StatType.CRI].SetBaseValue(25);
            originalStatMap[StatType.CRI].SetAdditionalValue(10);
            originalStatMap[StatType.CDMG].SetBaseValue(50);

            // Act - Round trip through List format
            var serialized = originalStatMap.Serialize();
            var deserialized = new StatMap((IValue)serialized);

            // Assert
            Assert.Equal(originalStatMap.HP, deserialized.HP);
            Assert.Equal(originalStatMap.BaseHP, deserialized.BaseHP);
            Assert.Equal(originalStatMap.AdditionalHP, deserialized.AdditionalHP);
            Assert.Equal(originalStatMap.ATK, deserialized.ATK);
            Assert.Equal(originalStatMap.BaseATK, deserialized.BaseATK);
            Assert.Equal(originalStatMap.AdditionalATK, deserialized.AdditionalATK);
            Assert.Equal(originalStatMap.DEF, deserialized.DEF);
            Assert.Equal(originalStatMap.BaseDEF, deserialized.BaseDEF);
            Assert.Equal(originalStatMap.AdditionalDEF, deserialized.AdditionalDEF);
            Assert.Equal(originalStatMap.CRI, deserialized.CRI);
            Assert.Equal(originalStatMap.BaseCRI, deserialized.BaseCRI);
            Assert.Equal(originalStatMap.AdditionalCRI, deserialized.AdditionalCRI);
            Assert.Equal(originalStatMap.CDMG, deserialized.CDMG);
            Assert.Equal(originalStatMap.BaseCDMG, deserialized.BaseCDMG);
            Assert.Equal(originalStatMap.AdditionalCDMG, deserialized.AdditionalCDMG);
        }
    }
}
