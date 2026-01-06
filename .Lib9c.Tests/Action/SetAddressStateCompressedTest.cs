namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Bencodex;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Xunit;

    /// <summary>
    /// Tests for SetAddressStateCompressed action.
    /// </summary>
    public class SetAddressStateCompressedTest
    {
        [Fact]
        public void CompressState_WithValidState_ShouldReturnCompressedData()
        {
            // Arrange
            var state = Dictionary.Empty
                .Add("key1", (Text)"value1")
                .Add("key2", new List(new IValue[] { (Integer)1, (Integer)2, (Integer)3 }))
                .Add("key3", Dictionary.Empty.Add("nested", (Text)"value"));

            // Act
            var compressedData = SetAddressStateCompressed.CompressState(state);

            // Assert
            Assert.True(compressedData.Length > 0);
        }

        [Fact]
        public void DecompressState_WithValidData_ShouldRestoreOriginal()
        {
            // Arrange
            var originalState = Dictionary.Empty
                .Add("key1", (Text)"value1")
                .Add("key2", new List(new IValue[] { (Integer)1, (Integer)2, (Integer)3 }))
                .Add("key3", Dictionary.Empty.Add("nested", (Text)"value"));

            var compressedData = SetAddressStateCompressed.CompressState(originalState);

            // Act
            var decompressedState = SetAddressStateCompressed.DecompressState(compressedData);

            // Assert
            Assert.Equal(originalState, decompressedState);
        }

        [Fact]
        public void DecompressState_WithInvalidData_ThrowsInvalidDataException()
        {
            var invalidData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            Assert.Throws<InvalidDataException>(() => SetAddressStateCompressed.DecompressState(invalidData));
        }

        [Fact]
        public void Execute_WithValidStates_ShouldSetStatesCorrectly()
        {
            // Arrange
            var states = new List<(Address, Address, IValue)>
            {
                (new PrivateKey().Address, new PrivateKey().Address, (Text)"state1"),
                (new PrivateKey().Address, new PrivateKey().Address, (Text)"state2"),
                (new PrivateKey().Address, new PrivateKey().Address, (Text)"state3"),
            };

            var action = new SetAddressStateCompressed(states);
            var context = new ActionContext
            {
                PreviousState = new World(MockUtil.MockModernWorldState),
                Signer = new PrivateKey().Address,
                BlockIndex = 1,
            };

            // Act
            var result = action.Execute(context);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Execute_WithExistingState_ShouldThrowException()
        {
            // Arrange
            var accountAddress = new PrivateKey().Address;
            var targetAddress = new PrivateKey().Address;
            var existingState = (Text)"existing_state";
            var newState = (Text)"new_state";

            var states = new List<(Address, Address, IValue)>
            {
                (accountAddress, targetAddress, newState),
            };

            var action = new SetAddressStateCompressed(states);
            var initialState = new World(MockUtil.MockModernWorldState);
            var account = initialState.GetAccount(accountAddress);
            account = account.SetState(targetAddress, existingState);
            var updatedState = initialState.SetAccount(accountAddress, account);

            var context = new ActionContext
            {
                PreviousState = updatedState,
                Signer = new PrivateKey().Address,
                BlockIndex = 1,
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => action.Execute(context));
        }

        [Fact]
        public void PlainValue_ShouldContainCompressedData()
        {
            // Arrange
            var states = new List<(Address, Address, IValue)>
            {
                (new PrivateKey().Address, new PrivateKey().Address, (Text)"state1"),
                (new PrivateKey().Address, new PrivateKey().Address, (Text)"state2"),
            };

            var action = new SetAddressStateCompressed(states);

            // Act
            var plainValue = action.PlainValue;

            // Assert
            Assert.NotNull(plainValue);
            Assert.IsType<Dictionary>(plainValue);
            var dictionary = (Dictionary)plainValue;
            Assert.True(dictionary.ContainsKey("type_id"));
            Assert.True(dictionary.ContainsKey("values"));
        }

        [Fact]
        public void LoadPlainValue_ShouldRestoreStates()
        {
            // Arrange
            var originalStates = new List<(Address, Address, IValue)>
            {
                (new PrivateKey().Address, new PrivateKey().Address, (Text)"state1"),
                (new PrivateKey().Address, new PrivateKey().Address, (Text)"state2"),
            };

            var action = new SetAddressStateCompressed(originalStates);
            var plainValue = action.PlainValue;

            // Act
            var newAction = new SetAddressStateCompressed();
            newAction.LoadPlainValue(plainValue);

            // Assert
            Assert.Equal(originalStates.Count, newAction.CompressedStates.Count);
            for (int i = 0; i < originalStates.Count; i++)
            {
                Assert.Equal(originalStates[i].Item1, newAction.CompressedStates[i].accountAddress);
                Assert.Equal(originalStates[i].Item2, newAction.CompressedStates[i].targetAddress);

                // Verify compressed states are properly stored
                Assert.NotNull(newAction.CompressedStates[i].compressedState);
                Assert.True(newAction.CompressedStates[i].compressedState.Length > 0);

                // Verify data integrity by decompressing and comparing
                var decompressedState = SetAddressStateCompressed.DecompressState(newAction.CompressedStates[i].compressedState);
                Assert.Equal(originalStates[i].Item3, decompressedState);
            }
        }

        [Fact]
        public void Compression_ShouldReduceSize_WithLargeData()
        {
            // Arrange - Create a large state with repetitive data
            var largeData = new List<IValue>();
            for (int i = 0; i < 100; i++)
            {
                largeData.Add((Text)($"This is a very long string with repetitive content that should compress well. Iteration {i}. " +
                             "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                             "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."));
            }

            var largeState = new List(largeData);

            var states = new List<(Address, Address, IValue)>
            {
                (new PrivateKey().Address, new PrivateKey().Address, largeState),
            };

            var action = new SetAddressStateCompressed(states);
            var plainValue = action.PlainValue;

            // Act - Get the size of compressed vs uncompressed
            var compressedSize = action.CompressedStates[0].compressedState.Length;
            var uncompressedSize = new Codec().Encode(largeState).Length;

            // Assert
            Assert.True(
                compressedSize < uncompressedSize,
                $"Compressed size ({compressedSize}) should be smaller than uncompressed size ({uncompressedSize})");

            // Verify the data integrity after compression/decompression
            var decompressedState = SetAddressStateCompressed.DecompressState(action.CompressedStates[0].compressedState);
            Assert.Equal(largeState, decompressedState);
        }

        [Fact]
        public void PlainValue_ShouldContainCompressedBinaryData()
        {
            // Arrange
            var states = new List<(Address, Address, IValue)>
            {
                (new PrivateKey().Address, new PrivateKey().Address, (Text)"test_state"),
            };

            var action = new SetAddressStateCompressed(states);
            var plainValue = action.PlainValue;

            // Act
            var dictionary = (Dictionary)plainValue;
            var values = (Dictionary)dictionary["values"];
            var statesList = (List)values["s"];
            var firstState = (List)statesList[0];
            var compressedData = (Binary)firstState[2];

            // Assert
            Assert.True(compressedData.ByteArray.Length > 0);

            // Verify it's actually compressed data (can be decompressed)
            var decompressedState = SetAddressStateCompressed.DecompressState(compressedData.ToByteArray());
            Assert.Equal(states[0].Item3, decompressedState);
        }

        [Fact]
        public void Constructor_WithNullStates_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SetAddressStateCompressed((IReadOnlyList<(Address, Address, IValue)>)null));
        }

        [Fact]
        public void Constructor_WithEmptyStates_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new SetAddressStateCompressed(new List<(Address, Address, IValue)>()));
        }

        [Fact]
        public void Constructor_WithNullStateValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            var states = new List<(Address, Address, IValue)>
            {
                (new PrivateKey().Address, new PrivateKey().Address, null),
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SetAddressStateCompressed(states));
        }

        [Fact]
        public void Constructor_WithNullCompressedStates_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SetAddressStateCompressed((IReadOnlyList<(Address, Address, byte[])>)null));
        }

        [Fact]
        public void Constructor_WithEmptyCompressedStates_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new SetAddressStateCompressed(new List<(Address, Address, byte[])>()));
        }

        [Fact]
        public void Constructor_WithNullCompressedState_ShouldThrowArgumentNullException()
        {
            // Arrange
            var compressedStates = new List<(Address, Address, byte[])>
            {
                (new PrivateKey().Address, new PrivateKey().Address, null),
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SetAddressStateCompressed(compressedStates));
        }

        [Fact]
        public void Constructor_WithEmptyCompressedState_ShouldThrowArgumentException()
        {
            // Arrange
            var compressedStates = new List<(Address, Address, byte[])>
            {
                (new PrivateKey().Address, new PrivateKey().Address, new byte[0]),
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new SetAddressStateCompressed(compressedStates));
        }

        [Fact]
        public void LoadPlainValue_WithNullPlainValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            var action = new SetAddressStateCompressed();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => action.LoadPlainValue(null));
        }

        [Fact]
        public void LoadPlainValue_WithInvalidFormat_ShouldThrowArgumentException()
        {
            // Arrange
            var action = new SetAddressStateCompressed();
            var invalidPlainValue = Dictionary.Empty.Add("invalid", (Text)"format");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => action.LoadPlainValue(invalidPlainValue));
        }

        [Fact]
        public void Execute_WithNullContext_ShouldThrowArgumentNullException()
        {
            // Arrange
            var states = new List<(Address, Address, IValue)>
            {
                (new PrivateKey().Address, new PrivateKey().Address, (Text)"test_state"),
            };
            var action = new SetAddressStateCompressed(states);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => action.Execute(null));
        }

        [Fact]
        public void CompressState_WithNullState_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SetAddressStateCompressed.CompressState(null));
        }

        [Fact]
        public void DecompressState_WithNullData_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SetAddressStateCompressed.DecompressState(null));
        }

        [Fact]
        public void DecompressState_WithEmptyData_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => SetAddressStateCompressed.DecompressState(new byte[0]));
        }

        [Fact]
        public void Execute_WithInventoryFixtureData_ShouldCompressAndSetState()
        {
            // Arrange - Load inventory fixture data
            var inventoryData = LoadInventoryFixtureData();
            var accountAddress = new PrivateKey().Address;
            var targetAddress = new PrivateKey().Address;

            var states = new List<(Address, Address, IValue)>
            {
                (accountAddress, targetAddress, inventoryData),
            };

            var action = new SetAddressStateCompressed(states);
            var context = new ActionContext
            {
                PreviousState = new World(MockUtil.MockModernWorldState),
                Signer = new PrivateKey().Address,
                BlockIndex = 1,
            };

            // Act
            var result = action.Execute(context);

            // Assert
            Assert.NotNull(result);

            // Verify the state was set correctly
            var account = result.GetAccount(accountAddress);
            var setState = account.GetState(targetAddress);
            var rawInventory = Assert.IsType<List>(setState);
            Assert.Equal(inventoryData, setState);
            var inventory = new Inventory(rawInventory);
            Assert.True(inventory.Items.Count > 0);

            // Verify compression was effective
            var compressedSize = action.CompressedStates[0].compressedState.Length;
            var originalSize = new Codec().Encode(inventoryData).Length;
            Assert.True(
                compressedSize < originalSize,
                $"Compressed size ({compressedSize}) should be smaller than original size ({originalSize})");
        }

        [Fact]
        public void Execute_WithLargeInventoryData_ShouldHandleCompressionEfficiently()
        {
            // Arrange - Create large inventory data similar to fixture
            var largeInventoryData = CreateLargeInventoryData();
            var accountAddress = new PrivateKey().Address;
            var targetAddress = new PrivateKey().Address;

            var states = new List<(Address, Address, IValue)>
            {
                (accountAddress, targetAddress, largeInventoryData),
            };

            var action = new SetAddressStateCompressed(states);

            // Act - Test compression efficiency
            var compressedSize = action.CompressedStates[0].compressedState.Length;
            var originalSize = new Codec().Encode(largeInventoryData).Length;
            var compressionRatio = (double)compressedSize / originalSize;

            // Assert
            Assert.True(
                compressionRatio < 0.8,
                $"Compression ratio ({compressionRatio:P}) should be less than 80% for large data");

            // Verify data integrity
            var decompressedData = SetAddressStateCompressed.DecompressState(action.CompressedStates[0].compressedState);
            Assert.Equal(largeInventoryData, decompressedData);
        }

        [Fact]
        public void Serialize_WithInventoryData_ShouldMaintainDataIntegrity()
        {
            // Arrange
            var inventoryData = LoadInventoryFixtureData();
            var accountAddress = new PrivateKey().Address;
            var targetAddress = new PrivateKey().Address;

            var originalStates = new List<(Address, Address, IValue)>
            {
                (accountAddress, targetAddress, inventoryData),
            };

            var action = new SetAddressStateCompressed(originalStates);
            var plainValue = action.PlainValue;

            // Act
            var newAction = new SetAddressStateCompressed();
            newAction.LoadPlainValue(plainValue);

            // Assert
            Assert.Equal(originalStates.Count, newAction.CompressedStates.Count);

            // Verify addresses
            Assert.Equal(originalStates[0].Item1, newAction.CompressedStates[0].accountAddress);
            Assert.Equal(originalStates[0].Item2, newAction.CompressedStates[0].targetAddress);

            // Verify data integrity through decompression
            var decompressedData = SetAddressStateCompressed.DecompressState(newAction.CompressedStates[0].compressedState);
            Assert.Equal(inventoryData, decompressedData);
        }

        [Fact]
        public void Decompression_ShouldRestoreExactOriginalData()
        {
            // Arrange
            var originalState = Dictionary.Empty
                .Add("key1", (Text)"value1")
                .Add("key2", new List(new IValue[] { (Integer)1, (Integer)2, (Integer)3 }))
                .Add("key3", Dictionary.Empty.Add("nested", (Text)"value"));

            // Act
            var compressedData = SetAddressStateCompressed.CompressState(originalState);
            var decompressedState = SetAddressStateCompressed.DecompressState(compressedData);

            // Assert
            Assert.Equal(originalState, decompressedState);
        }

        [Fact]
        public void Decompression_WithInventoryFixtureData()
        {
            var inventoryData = LoadInventoryFixtureData();
            var inventory = new Inventory((List)inventoryData);
            var compressed = SetAddressStateCompressed.CompressState(inventoryData);
            var decompressed = SetAddressStateCompressed.DecompressState(compressed);
            var rawInventory = Assert.IsType<List>(decompressed);
            Assert.Equal(inventoryData, rawInventory);
            var newInventory = new Inventory(rawInventory);
            Assert.Equal(inventory.Items.Count, newInventory.Items.Count);
        }

        [Fact]
        public void Execute_ShouldUseCompressedData()
        {
            // Arrange
            var states = new List<(Address, Address, IValue)>
            {
                (new PrivateKey().Address, new PrivateKey().Address, (Text)"compressed_state"),
            };

            var action = new SetAddressStateCompressed(states);

            // Act & Assert
            // Verify that CompressedStates contains actual compressed data
            Assert.NotNull(action.CompressedStates);
            Assert.Single(action.CompressedStates);
            Assert.NotNull(action.CompressedStates[0].compressedState);
            Assert.True(action.CompressedStates[0].compressedState.Length > 0);

            // Verify that the compressed data is different from the original
            var originalData = new Codec().Encode(states[0].Item3);
            Assert.NotEqual(originalData, action.CompressedStates[0].compressedState);
        }

        [Fact]
        public void Execute_WithFixtureData_ShouldProduceSameResultAsSetAddressState()
        {
            // Arrange - Load inventory fixture data
            var inventoryData = LoadInventoryFixtureData();
            var accountAddress = new PrivateKey().Address;
            var targetAddress = new PrivateKey().Address;

            var states = new List<(Address, Address, IValue)>
            {
                (accountAddress, targetAddress, inventoryData),
            };

            // Create both actions with the same data
            var compressedAction = new SetAddressStateCompressed(states);
            var regularAction = new SetAddressState(states);

            var context = new ActionContext
            {
                PreviousState = new World(MockUtil.MockModernWorldState),
                Signer = new PrivateKey().Address,
                BlockIndex = 1,
            };

            // Act - Execute both actions
            var compressedResult = compressedAction.Execute(context);
            var regularResult = regularAction.Execute(context);

            // Assert - Both should produce the same final state
            var compressedAccount = compressedResult.GetAccount(accountAddress);
            var regularAccount = regularResult.GetAccount(accountAddress);

            var compressedState = compressedAccount.GetState(targetAddress);
            var regularState = regularAccount.GetState(targetAddress);

            // Verify both actions set the same state
            Assert.NotNull(compressedState);
            Assert.NotNull(regularState);
            Assert.Equal(regularState, compressedState);

            // Verify the state is the original inventory data
            Assert.Equal(inventoryData, compressedState);
            Assert.Equal(inventoryData, regularState);
        }

        [Fact]
        public void Execute_WithLargeData_ShouldProduceSameResultAsSetAddressState()
        {
            // Arrange - Create large inventory data
            var largeInventoryData = CreateLargeInventoryData();
            var accountAddress = new PrivateKey().Address;
            var targetAddress = new PrivateKey().Address;

            var states = new List<(Address, Address, IValue)>
            {
                (accountAddress, targetAddress, largeInventoryData),
            };

            // Create both actions with the same data
            var compressedAction = new SetAddressStateCompressed(states);
            var regularAction = new SetAddressState(states);

            var context = new ActionContext
            {
                PreviousState = new World(MockUtil.MockModernWorldState),
                Signer = new PrivateKey().Address,
                BlockIndex = 1,
            };

            // Act - Execute both actions
            var compressedResult = compressedAction.Execute(context);
            var regularResult = regularAction.Execute(context);

            // Assert - Both should produce the same final state
            var compressedAccount = compressedResult.GetAccount(accountAddress);
            var regularAccount = regularResult.GetAccount(accountAddress);

            var compressedState = compressedAccount.GetState(targetAddress);
            var regularState = regularAccount.GetState(targetAddress);

            // Verify both actions set the same state
            Assert.NotNull(compressedState);
            Assert.NotNull(regularState);
            Assert.Equal(regularState, compressedState);

            // Verify the state is the original large inventory data
            Assert.Equal(largeInventoryData, compressedState);
            Assert.Equal(largeInventoryData, regularState);
        }

        [Fact]
        public void Execute_WithMultipleStates_ShouldProduceSameResultAsSetAddressState()
        {
            // Arrange - Create multiple states with different data
            var states = new List<(Address, Address, IValue)>
            {
                (new PrivateKey().Address, new PrivateKey().Address, LoadInventoryFixtureData()),
                (new PrivateKey().Address, new PrivateKey().Address, CreateLargeInventoryData()),
                (new PrivateKey().Address, new PrivateKey().Address, (Text)"simple_text_state"),
                (new PrivateKey().Address, new PrivateKey().Address, Dictionary.Empty.Add("key", (Text)"value")),
            };

            // Create both actions with the same data
            var compressedAction = new SetAddressStateCompressed(states);
            var regularAction = new SetAddressState(states);

            var context = new ActionContext
            {
                PreviousState = new World(MockUtil.MockModernWorldState),
                Signer = new PrivateKey().Address,
                BlockIndex = 1,
            };

            // Act - Execute both actions
            var compressedResult = compressedAction.Execute(context);
            var regularResult = regularAction.Execute(context);

            // Assert - Both should produce the same final state for all addresses
            for (int i = 0; i < states.Count; i++)
            {
                var (accountAddress, targetAddress, _) = states[i];

                var compressedAccount = compressedResult.GetAccount(accountAddress);
                var regularAccount = regularResult.GetAccount(accountAddress);

                var compressedState = compressedAccount.GetState(targetAddress);
                var regularState = regularAccount.GetState(targetAddress);

                // Verify both actions set the same state
                Assert.NotNull(compressedState);
                Assert.NotNull(regularState);
                Assert.Equal(regularState, compressedState);
            }
        }

        [Fact]
        public void PlainValue_WithFixtureData_ShouldHaveDifferentSizeButSameResult()
        {
            // Arrange
            var inventoryData = LoadInventoryFixtureData();
            var states = new List<(Address, Address, IValue)>
            {
                (new PrivateKey().Address, new PrivateKey().Address, inventoryData),
            };

            var compressedAction = new SetAddressStateCompressed(states);
            var regularAction = new SetAddressState(states);

            // Act
            var compressedPlainValue = compressedAction.PlainValue;
            var regularPlainValue = regularAction.PlainValue;

            // Assert - Compressed should be smaller
            var compressedSize = new Codec().Encode(compressedPlainValue).Length;
            var regularSize = new Codec().Encode(regularPlainValue).Length;

            Assert.True(
                compressedSize < regularSize,
                $"Compressed size ({compressedSize}) should be smaller than regular size ({regularSize})");

            // Verify both can be loaded back to produce the same result
            var newCompressedAction = new SetAddressStateCompressed();
            var newRegularAction = new SetAddressState();

            newCompressedAction.LoadPlainValue(compressedPlainValue);
            newRegularAction.LoadPlainValue(regularPlainValue);

            // Both should have the same addresses
            Assert.Equal(regularAction.States.Count, newCompressedAction.CompressedStates.Count);
            Assert.Equal(regularAction.States[0].Item1, newCompressedAction.CompressedStates[0].accountAddress);
            Assert.Equal(regularAction.States[0].Item2, newCompressedAction.CompressedStates[0].targetAddress);

            // Verify the decompressed data matches the original
            var decompressedData = SetAddressStateCompressed.DecompressState(newCompressedAction.CompressedStates[0].compressedState);
            Assert.Equal(regularAction.States[0].Item3, decompressedData);
        }

        /// <summary>
        /// Loads inventory fixture data from the test fixtures directory.
        /// </summary>
        /// <returns>IValue representing the inventory data.</returns>
        private static IValue LoadInventoryFixtureData()
        {
            var fixturePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFixtures", "inventory_sample.txt");
            if (!File.Exists(fixturePath))
            {
                throw new FileNotFoundException($"Inventory fixture file not found at {fixturePath}");
            }

            var hexData = File.ReadAllText(fixturePath).Trim();
            var bytes = ByteUtil.ParseHex(hexData);

            // Try to decode as Bencodex data
            try
            {
                return new Codec().Decode(bytes);
            }
            catch
            {
                // If decoding fails, wrap as Binary data
                return new Binary(bytes);
            }
        }

        /// <summary>
        /// Creates large inventory data for compression testing.
        /// </summary>
        /// <returns>IValue representing large inventory data.</returns>
        private static IValue CreateLargeInventoryData()
        {
            var items = new List<IValue>();

            // Create repetitive inventory data that compresses well
            for (int i = 0; i < 100; i++)
            {
                var item = Dictionary.Empty
                    .Add("id", (Integer)(300000 + i))
                    .Add("count", (Integer)(i + 1))
                    .Add("level", (Integer)(i % 10 + 1))
                    .Add("exp", (Integer)(i * 100))
                    .Add("description", (Text)$"This is a test item with ID {300000 + i} and some repetitive content that should compress well when repeated many times.");

                items.Add(item);
            }

            return new List(items);
        }
    }
}
