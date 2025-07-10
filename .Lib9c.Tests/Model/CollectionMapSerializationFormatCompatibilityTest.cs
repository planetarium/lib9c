namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Xunit;

    /// <summary>
    /// Tests for CollectionMap serialization format compatibility.
    /// Ensures backward compatibility between Dictionary and List formats.
    /// </summary>
    public class CollectionMapSerializationFormatCompatibilityTest
    {
        [Fact]
        public void Serialize_ReturnsListFormat()
        {
            // Arrange
            var collectionMap = new CollectionMap();
            collectionMap.Add(1, 100);
            collectionMap.Add(2, 200);
            collectionMap.Add(3, 300);

            // Act
            var serialized = collectionMap.Serialize();

            // Assert
            Assert.IsType<List>(serialized);
            var list = (List)serialized;
            Assert.Equal(2, list.Count); // version + entries
            Assert.Equal(1, ((Integer)list[0]).Value); // version
            Assert.IsType<List>(list[1]); // entries
        }

        [Fact]
        public void Deserialize_SupportsBothFormats()
        {
            // Arrange
            var collectionMap = new CollectionMap();
            collectionMap.Add(1, 100);
            collectionMap.Add(2, 200);
            collectionMap.Add(3, 300);

            // Act - Test List format
            var listSerialized = collectionMap.Serialize();
            var deserializedFromList = new CollectionMap((IValue)listSerialized);

            // Act - Test Dictionary format (legacy)
            var dictSerialized = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text)"1", (Text)"100"),
                new KeyValuePair<IKey, IValue>((Text)"2", (Text)"200"),
                new KeyValuePair<IKey, IValue>((Text)"3", (Text)"300"),
            });
            var deserializedFromDict = new CollectionMap((IValue)dictSerialized);

            // Assert
            Assert.Equal(collectionMap.Count, deserializedFromList.Count);
            Assert.Equal(collectionMap[1], deserializedFromList[1]);
            Assert.Equal(collectionMap[2], deserializedFromList[2]);
            Assert.Equal(collectionMap[3], deserializedFromList[3]);

            Assert.Equal(collectionMap.Count, deserializedFromDict.Count);
            Assert.Equal(collectionMap[1], deserializedFromDict[1]);
            Assert.Equal(collectionMap[2], deserializedFromDict[2]);
            Assert.Equal(collectionMap[3], deserializedFromDict[3]);
        }

        [Fact]
        public void Deserialize_EmptyCollectionMap_WorksCorrectly()
        {
            // Arrange
            var collectionMap = new CollectionMap();

            // Act - Test List format
            var listSerialized = collectionMap.Serialize();
            var deserializedFromList = new CollectionMap((IValue)listSerialized);

            // Act - Test Dictionary format (legacy)
            var dictSerialized = (IValue)Dictionary.Empty;
            var deserializedFromDict = new CollectionMap(dictSerialized);

            // Assert
            Assert.Empty(deserializedFromList);
            Assert.Empty(deserializedFromDict);
        }

        [Fact]
        public void Deserialize_InvalidFormat_ThrowsArgumentException()
        {
            // Arrange
            var invalidSerialized = (IValue)new Text("invalid");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new CollectionMap(invalidSerialized));
            Assert.Contains("Unsupported serialization format", exception.Message);
        }

        [Fact]
        public void Deserialize_InvalidListLength_ThrowsArgumentException()
        {
            // Arrange
            var invalidList = List.Empty.Add(1); // Only version, missing entries

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new CollectionMap((IValue)invalidList));
            Assert.Contains("Invalid list length", exception.Message);
        }

        [Fact]
        public void Deserialize_InvalidVersion_ThrowsArgumentException()
        {
            // Arrange
            var invalidVersionList = List.Empty
                .Add(999) // Invalid version
                .Add(List.Empty); // Empty entries

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new CollectionMap((IValue)invalidVersionList));
            Assert.Contains("Unsupported serialization version", exception.Message);
        }

        [Fact]
        public void Serialize_EntriesAreOrderedByKey()
        {
            // Arrange
            var collectionMap = new CollectionMap();
            collectionMap.Add(3, 300);
            collectionMap.Add(1, 100);
            collectionMap.Add(2, 200);

            // Act
            var serialized = collectionMap.Serialize();
            var list = (List)serialized;
            var entriesList = (List)list[1];

            // Assert
            Assert.Equal(3, entriesList.Count);

            // Entries should be ordered by key
            var firstEntry = (List)entriesList[0];
            var secondEntry = (List)entriesList[1];
            var thirdEntry = (List)entriesList[2];

            Assert.Equal(1, firstEntry[0].ToInteger());
            Assert.Equal(100, firstEntry[1].ToInteger());
            Assert.Equal(2, secondEntry[0].ToInteger());
            Assert.Equal(200, secondEntry[1].ToInteger());
            Assert.Equal(3, thirdEntry[0].ToInteger());
            Assert.Equal(300, thirdEntry[1].ToInteger());
        }

        [Fact]
        public void RoundTrip_ComplexCollectionMap_WorksCorrectly()
        {
            // Arrange
            var originalCollectionMap = new CollectionMap();
            originalCollectionMap.Add(1, 100);
            originalCollectionMap.Add(5, 500);
            originalCollectionMap.Add(10, 1000);
            originalCollectionMap.Add(2, 200);
            originalCollectionMap.Add(8, 800);

            // Act - Round trip through List format
            var serialized = originalCollectionMap.Serialize();
            var deserialized = new CollectionMap((IValue)serialized);

            // Assert
            Assert.Equal(originalCollectionMap.Count, deserialized.Count);
            Assert.Equal(originalCollectionMap[1], deserialized[1]);
            Assert.Equal(originalCollectionMap[2], deserialized[2]);
            Assert.Equal(originalCollectionMap[5], deserialized[5]);
            Assert.Equal(originalCollectionMap[8], deserialized[8]);
            Assert.Equal(originalCollectionMap[10], deserialized[10]);
        }

        [Fact]
        public void Deserialize_LegacyDictionaryFormat_WorksCorrectly()
        {
            // Arrange - Legacy Dictionary format
            var dictSerialized = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text)"42", (Text)"420"),
                new KeyValuePair<IKey, IValue>((Text)"7", (Text)"70"),
                new KeyValuePair<IKey, IValue>((Text)"15", (Text)"150"),
            });

            // Act
            var deserialized = new CollectionMap((IValue)dictSerialized);

            // Assert
            Assert.Equal(3, deserialized.Count);
            Assert.Equal(420, deserialized[42]);
            Assert.Equal(70, deserialized[7]);
            Assert.Equal(150, deserialized[15]);
        }

        [Fact]
        public void Deserialize_NullValue_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new CollectionMap((IValue)null));
            Assert.Equal("serialized", exception.ParamName);
        }

        [Fact]
        public void Add_ExistingKey_AddsValues()
        {
            // Arrange
            var collectionMap = new CollectionMap();
            collectionMap.Add(1, 100);

            // Act
            collectionMap.Add(1, 50);

            // Assert
            Assert.Equal(150, collectionMap[1]);
        }

        [Fact]
        public void Add_NewKey_AddsNewEntry()
        {
            // Arrange
            var collectionMap = new CollectionMap();

            // Act
            collectionMap.Add(1, 100);

            // Assert
            Assert.Single(collectionMap);
            Assert.Equal(100, collectionMap[1]);
        }

        [Fact]
        public void Serialize_WithAddOperation_PreservesData()
        {
            // Arrange
            var collectionMap = new CollectionMap();
            collectionMap.Add(1, 100);
            collectionMap.Add(1, 50); // Should result in 150

            // Act
            var serialized = collectionMap.Serialize();
            var deserialized = new CollectionMap((IValue)serialized);

            // Assert
            Assert.Single(deserialized);
            Assert.Equal(150, deserialized[1]);
        }
    }
}
