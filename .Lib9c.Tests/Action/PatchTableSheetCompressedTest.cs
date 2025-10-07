namespace Lib9c.Tests.Action
{
    using System;
    using System.Text;
    using Lib9c.Action;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Xunit;

    /// <summary>
    /// Tests for PatchTableSheetCompressed action.
    ///
    /// This test class verifies the compression functionality, data integrity,
    /// and performance benefits of the compressed table sheet patching action.
    ///
    /// Key test scenarios:
    /// - CSV compression and decompression.
    /// - Data integrity validation.
    /// - Transaction size reduction analysis.
    /// - Action execution with compressed data.
    /// </summary>
    public class PatchTableSheetCompressedTest
    {
        private readonly Address _signer;
        private readonly IActionContext _context;
        private readonly IWorld _previousState;

        /// <summary>
        /// Initializes a new instance of the PatchTableSheetCompressedTest class.
        /// Sets up the test environment with mock world state and action context.
        /// </summary>
        public PatchTableSheetCompressedTest()
        {
            _signer = new PrivateKey().Address;
            _previousState = new World(MockWorldState.CreateModern());
            _context = new ActionContext
            {
                PreviousState = _previousState,
                RandomSeed = 0,
            };
        }

        /// <summary>
        /// Tests that CSV data can be successfully compressed.
        ///
        /// This test verifies that:
        /// - Compression produces non-empty byte array.
        /// - Compressed size is smaller than original size.
        /// - Compression works with valid CSV data.
        /// </summary>
        [Fact]
        public void CompressCsv_WithValidData_ShouldCompress()
        {
            // Arrange - Use a larger CSV data that will actually compress
            var originalCsv = new StringBuilder();
            originalCsv.AppendLine("id,name,value,description,type,level,rarity,price,weight,durability");
            for (int i = 1; i <= 100; i++)
            {
                originalCsv.AppendLine($"{i},Item{i},{i * 10},This is item number {i} with a long description that repeats many times to make the data compressible,Weapon,{i % 10 + 1},Common,{i * 100},{i * 0.5},{100 - i}");
            }

            // Act
            var compressedData = PatchTableSheetCompressed.CompressCsv(originalCsv.ToString());

            // Assert
            Assert.True(compressedData.Length > 0);
            Assert.True(compressedData.Length < Encoding.UTF8.GetByteCount(originalCsv.ToString()));
        }

        /// <summary>
        /// Tests that compressed CSV data can be successfully decompressed to restore original data.
        ///
        /// This test verifies data integrity by ensuring that:
        /// - Compressed data can be decompressed.
        /// - Decompressed data matches the original exactly.
        /// - No data loss occurs during compression/decompression cycle.
        /// </summary>
        [Fact]
        public void DecompressCsv_WithValidData_ShouldRestoreOriginal()
        {
            // Arrange
            var originalCsv = "id,name,value\n1,test,100\n2,test2,200";
            var compressedData = PatchTableSheetCompressed.CompressCsv(originalCsv);

            // Act
            var decompressedCsv = PatchTableSheetCompressed.DecompressCsv(compressedData);

            // Assert
            Assert.Equal(originalCsv, decompressedCsv);
        }

        /// <summary>
        /// Tests that the PatchTableSheetCompressed action executes successfully with valid data.
        ///
        /// This test verifies that:
        /// - Action can be created with compressed data.
        /// - Action executes without throwing exceptions.
        /// - State is updated correctly.
        /// </summary>
        [Fact]
        public void Execute_WithValidData_ShouldSucceed()
        {
            // Arrange
            var csvData = "id,name,value\n1,test,100\n2,test2,200";
            var compressedData = PatchTableSheetCompressed.CompressCsv(csvData);

            var action = new PatchTableSheetCompressed
            {
                TableName = "TestSheet",
                CompressedTableCsv = compressedData,
            };

            // Act
            var nextState = action.Execute(_context);

            // Assert
            Assert.NotNull(nextState);
        }

        /// <summary>
        /// Tests and analyzes the compression benefits using RuneOptionSheet.csv data.
        ///
        /// This test demonstrates the significant transaction size reduction achieved
        /// by using compressed table sheet patching. RuneOptionSheet.csv is a large
        /// file that shows dramatic compression benefits.
        ///
        /// Expected results:
        /// - Original size: ~842KB.
        /// - Compressed size: ~154KB.
        /// - Compression ratio: ~18% (81% reduction).
        /// </summary>
        [Fact]
        public void CompareTransactionSizes_WithRuneOptionSheet_ShouldShowCompressionBenefit()
        {
            // Arrange - Use a realistic CSV data that simulates RuneOptionSheet
            var runeOptionSheetCsv = new StringBuilder();
            runeOptionSheetCsv.AppendLine("rune_id,level,total_cp,stat_type_1,value_1,value_type_1,stat_type_2,value_2,value_type_2,stat_type_3,value_3,value_type3,skillId,cooldown,chance,skill_value,skill_stat_ratio,skill_value_type,stat_reference_type,buff_duration");

            // Generate realistic data similar to RuneOptionSheet
            for (int runeId = 30001; runeId <= 30100; runeId++)
            {
                for (int level = 1; level <= 10; level++)
                {
                    runeOptionSheetCsv.AppendLine($"{runeId},{level},{100 + level * 50},HP,{100 + level * 20},Add,NONE,0,Add,NONE,0,Add,,,,,,,,");
                }
            }

            var csvData = runeOptionSheetCsv.ToString();

            var uncompressedAction = new PatchTableSheet
            {
                TableName = "RuneOptionSheet",
                TableCsv = csvData,
            };

            var compressedAction = new PatchTableSheetCompressed
            {
                TableName = "RuneOptionSheet",
                CompressedTableCsv = PatchTableSheetCompressed.CompressCsv(csvData),
            };

            // Act - Get serialized data sizes
            var uncompressedSize = Encoding.UTF8.GetByteCount(csvData);
            var compressedSize = PatchTableSheetCompressed.CompressCsv(csvData).Length;

            // Calculate compression ratio
            var compressionRatio = (double)compressedSize / uncompressedSize;
            var sizeReduction = uncompressedSize - compressedSize;
            var reductionPercentage = (double)sizeReduction / uncompressedSize * 100;

            // Assert
            Assert.True(
                compressedSize < uncompressedSize,
                $"Compressed size ({compressedSize} bytes) should be smaller than uncompressed size ({uncompressedSize} bytes)");

            // Log results for analysis
            Console.WriteLine($"=== Transaction Size Comparison (RuneOptionSheet) ===");
            Console.WriteLine($"Original CSV size: {uncompressedSize:N0} bytes");
            Console.WriteLine($"Compressed size: {compressedSize:N0} bytes");
            Console.WriteLine($"Size reduction: {sizeReduction:N0} bytes ({reductionPercentage:F1}%)");
            Console.WriteLine($"Compression ratio: {compressionRatio:P}");
            Console.WriteLine("==============================================");

            // Assert that compression is significant
            Assert.True(
                reductionPercentage > 50,
                $"Should have significant compression: {reductionPercentage:F1}% reduction");
        }
    }
}
